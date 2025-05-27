const http = require('http');
const https = require('https');

// START OF USER CONFIGURATION
// TRACEABLE_AGENT_TOKEN must be set, provided by traceable
const TRACEABLE_GUID = "b80952b5-9ddf-4e13-9912-c0048f8ee039";
const TRACEABLE_AGENT_TOKEN = "Y-Lq1ftZcFqFtZdrWW1WqzSj6M3M2x9FriJG13RyGu-rf";
// Environment name that will appear in platform, 
// if no environment name is given, Traceable Platform Agent's environment name will be used
// Service name that will appear in platform
const SERVICE_NAME = "cloudfront-crapi";
const TPA_HOST = "agents-dev.traceable.ai";
const TPA_PORT = 443;
// If TPA is running over http change protocol to http
const TPA_PROTOCOL = "https";
const TRACEABLE_LOG_PREFIX = "Traceable: ";

// Version of Traceable.js to proxy
const TRACEABLE_JS_VERSION = "latest";

// if INCLUDE_PATHS present, only report/block included urls
// if EXCLUDE_PATHS present, report/block everything except exclude
// if neither present trace everything
// Array of regexes for paths to include
const INCLUDE_PATH_REGEXES = [];
// Array of regexes for paths to exclude
const EXCLUDE_PATH_REGEXES = [];
// Array of regexes for paths to do captcha validation
const CAPTCHA_PATH_REGEXES = [];

// Timeout of ext_cap request to TPA in milliseconds
// This value should allow for following:
// TIMEOUT_MS * 2 < lambda-configured-timeout
// why? There are 2 timeouts, 1 for socket connection & 1 for completing request
// so in worst case (TIMEOUT_MS) * 2
// If TIMEOUT_MS * 2 >= lambda timeout, lambda will fail & cloudfront will return 503 to client
const TIMEOUT_MS = 700;
// If false trace header won't be injected, this will prevent traceable_viewer_response.js from associating request & response
const ADD_TRACE_HEADER = true;
// Header name added to request, if modified make sure to update in traceable_viewer_response.js as well
const TRACE_HEADER_NAME = "traceparent";

// If true will enable ext_cap to take cookie validation into account for block decision/header injections
const ENABLE_CAPTCHA = true;

const ENABLE_DEBUG_LOG = true;

const ENABLE_DEBUG_HEADER_INJECTION = false;

// Maximum sockets per lambda instance
const MAX_SOCKETS = 100;
// END OF USER CONFIGURATION

const MODULE_NAME = "lambda-edge";
const MODULE_VERSION = "1.0.0";
const EXT_CAP_PATH = "/ext_cap/v1/req_cap";
const GET_COOKIE_PATH = "/ext_cap/v1/get_traceable_cookie";
const VALIDATE_COOKIE_PATH = "/ext_cap/v1/validate_traceable_cookie";
const CREATE_SPAN_PATH = "/ext_cap/v1/create_span";
const DOWNLOAD_HOST = "downloads.traceable.ai"
const DOWNLOAD_PATH = "/agent/traceable-js/latest/traceable-js.js"
const PROTOCOL_COLON = TPA_PROTOCOL + ":";
const CAPTCHA_ERROR = "CAPTCHA_ERROR"
const CAPTCHA_ERROR_PREFIX = CAPTCHA_ERROR + "_";

const agentType = PROTOCOL_COLON === "https:" ? https : http
const agent = new agentType.Agent({keepAlive: true, maxSockets: MAX_SOCKETS});

const EXT_CAP_HEADERS = {
    "Content-Type": "application/json",
    "traceableai-module-name": MODULE_NAME,
    "traceableai-module-version": MODULE_VERSION,
    "traceableai-service-name": SERVICE_NAME,
    "traceableai-skip-merge-data-flag": "yes",
    "traceableai-agent-token": TRACEABLE_AGENT_TOKEN,
    "x-traceable-guid": TRACEABLE_GUID
};

const CAPTCHA_HEADERS = {
    "Content-Type": "application/json",
    "traceableai-agent-token": TRACEABLE_AGENT_TOKEN
}

const DEBUG_HEADERS = {
    started: "started",
    tpaCallFailure: "failed-tpa-call",
    tpaCallError: "errored-tpa-call",
    tpaCallCompleted: "completed-tpa-call",
    skipped: "skipped"
};

const DEBUG_HEADER_NAME = "traceable-lambda-edge-status"

// Pre-built regex objects for include and exclude paths
let _INCLUDE_PATH_REGEXES_BUILT = buildRegexs(INCLUDE_PATH_REGEXES)
let _EXCLUDE_PATH_REGEXES_BUILT = buildRegexs(EXCLUDE_PATH_REGEXES)
let _CAPTCHA_PATH_REGEXES_BUILT = buildRegexs(CAPTCHA_PATH_REGEXES)
let _CAPTCHA_JS_SCRIPT_CONTENT = null

// rootCAB64 only required if using self-signed certs for TPA deployment
const rootCAB64 = '';
const caBuf = rootCAB64.length > 0 ? Buffer.from(rootCAB64, 'base64') : ""

// If user enters bad regex that regex wont be applied but valid regexs will
function buildRegexs(regexArray) {
    return regexArray.map((pattern) => {
        try {
            return new RegExp(pattern)
        } catch (e) {
            console.error(`Error building regex: ${pattern}`);
            return null;
        }
    }).filter(regex => regex !== null);
}

const handler = async (event, context, callback) => {
    const originalRequest = event.Records[0].cf.request;
    injectDebugHeader(originalRequest, DEBUG_HEADERS.started);
    try {
        let uri = originalRequest.uri;

        if (uri === "/traceable/stepup") {
            debugLog("Received stepup call for request: " + JSON.stringify(originalRequest));
            try {
                let stepUpData = readStepUpData(originalRequest)

                if(!isCaptchaAppliedPath(stepUpData)){
                    return callback(null, makeStepUpResponse(false, []))
                }

                let additionalCookies = []

                let tpaResponse = await makeTpaCaptchaCall(stepUpData, originalRequest)
                if(tpaResponse.success){
                    debugLog(tpaResponse.data)
                    for(let i = 0; i < tpaResponse.data.setCookies.length; i++){
                        additionalCookies.push({key: 'Set-Cookie', value: tpaResponse.data.setCookies[i]})
                    }
                    // For getTraceableCookie calls we always return false, for validate calls rely on isValid field
                    const showCaptcha = tpaResponse.path === GET_COOKIE_PATH ? false : !tpaResponse.data.isValid
                    return callback(null, makeStepUpResponse(showCaptcha, additionalCookies))
                } else {
                    log("TPA request unsuccessful - status:" + tpaResponse.statusCode + " body: " + tpaResponse.body + " path: " + tpaResponse.path);
                }
            } catch(e) {
                log("hit error during /traceable/stepup - error: " + e);
                return callback(null, makeStepUpResponse(false, []));
            }

            return callback(null, makeStepUpResponse(false, []))

        } else if (uri === "/traceable/events") {
            debugLog("Received events call for request: " + JSON.stringify(originalRequest));
            try {
                let eventsData = readEventsData(originalRequest);
                let tpaResponse = await makeTpaEventsCall(eventsData);
                if(tpaResponse.success){
                    debugLog(tpaResponse.data);
                } else {
                    log("TPA request unsuccessful - status:" + tpaResponse.statusCode + " body: " + tpaResponse.body + " path: " + tpaResponse.path);
                }

            } catch(e) {
                log("hit error during /traceable/events - error: " + e);
            }
            return callback(null, makeEventsResponse());
        } else if (uri === "/traceable/captcha.js") {
            // This means we have never fetched it during this lambda lifecycle
            if(_CAPTCHA_JS_SCRIPT_CONTENT === null){
                const captchaResponse = await makeHttpCall({
                    host: DOWNLOAD_HOST,
                    port: 443,
                    path: DOWNLOAD_PATH,
                    method: 'GET',
                    headers: {},
                    protocol: PROTOCOL_COLON
                }, null)

                if(captchaResponse.statusCode < 400 && captchaResponse.body) {
                    _CAPTCHA_JS_SCRIPT_CONTENT = captchaResponse.body
                    return callback(null, makeCaptchaJsResponse(_CAPTCHA_JS_SCRIPT_CONTENT))
                } else {
                    return callback(null, {
                            statusCode: captchaResponse.statusCode
                        }
                    )
                }
            } else {
                // return cached version
                return callback(null, makeCaptchaJsResponse(_CAPTCHA_JS_SCRIPT_CONTENT))
            }

        }


        if (shouldSkip(uri)) {
            debugLog("Should skip static assets or uri did not match include/exclude rules");
            injectDebugHeader(originalRequest, DEBUG_HEADERS.skipped);
            return callback(null, originalRequest);
        }

        debugLog("Executing lambda edge script for request: " + JSON.stringify(originalRequest));
        const extCapData = makeReqCapBody(event);
        debugLog("Ext cap request: " + JSON.stringify(extCapData));

        const tpaResponse = await makeHttpCall({
            host: TPA_HOST,
            port: TPA_PORT,
            path: EXT_CAP_PATH,
            method: 'POST',
            headers: EXT_CAP_HEADERS,
            protocol: PROTOCOL_COLON,
            agent: agent,
        }, extCapData);

        if (tpaResponse.statusCode !== 200) {
            log("TPA response did not return 200, got: " + tpaResponse?.statusCode + " body: " + tpaResponse?.body + " path: " + tpaResponse?.path)
            injectDebugHeader(originalRequest, DEBUG_HEADERS.tpaCallFailure);
            // TODO add full response details
            return callback(null, originalRequest);
        }

        const tpaJson = JSON.parse(tpaResponse.body);
        debugLog("Ext cap response: "+ JSON.stringify(tpaJson));

        if (tpaJson["allowRequest"] === false) {
            return callback(null, makeBlockResponse(tpaJson));
        } else {
            addHeaders(originalRequest, tpaJson)
        }
    } catch (e) {
        injectDebugHeader(originalRequest, DEBUG_HEADERS.tpaCallError);
        log(e)
    }
    injectDebugHeader(originalRequest, DEBUG_HEADERS.tpaCallCompleted);
    return callback(null, originalRequest);
};


async function makeTpaCaptchaCall(stepUpData, originalRequest){
    let body = {};
    let cookieKeyValue = originalRequest.headers["cookie"];
    body["request_cookies"] = cookieKeyValue[0].value;
    body["page_url"] = stepUpData.url;
    body["browser_identifier"] = stepUpData.browser_identifier;
    let path = VALIDATE_COOKIE_PATH;;
    if(stepUpData.token) {
        body["captcha_token"] = stepUpData.token;
        path = GET_COOKIE_PATH;
    }

    return await makeCaptchaCall(path, body);
}

async function makeCaptchaCall(path, body){
    debugLog("Making call to tpa with this body: " + body);
    const tpaResponse = await makeHttpCall({
        host: TPA_HOST,
        port: TPA_PORT,
        path: path,
        method: 'POST',
        headers: CAPTCHA_HEADERS,
        protocol: PROTOCOL_COLON,
        agent: agent,
    }, body);

    if(tpaResponse.statusCode === 200){
        debugLog("TPA Response body: " + tpaResponse.body)
        return {
            statusCode: tpaResponse.statusCode,
            path: path,
            success: true,
            data: JSON.parse(tpaResponse.body)
        }
    } else {
        return {
            statusCode: tpaResponse.statusCode,
            body: tpaResponse.body,
            success: false,
            path: path
        }
    }
}

async function makeTpaEventsCall(body) {
    debugLog("Making call to tpa with this body: " + body)
    const tpaResponse = await makeHttpCall({
        host: TPA_HOST,
        port: TPA_PORT,
        path: CREATE_SPAN_PATH,
        method: 'POST',
        headers: CAPTCHA_HEADERS,
        protocol: PROTOCOL_COLON,
        agent: agent,
    }, body);

    if(tpaResponse.statusCode === 200){
        debugLog("TPA Response body: " + tpaResponse.body);
        return {
            statusCode: tpaResponse.statusCode,
            success: true,
            data: JSON.parse(tpaResponse.body)
        };
    } else {
        return {
            statusCode: tpaResponse.statusCode,
            body: tpaResponse.body,
            success: false,
        };
    }
}

function readHeadersFromRequest(originalRequest) {
    if(!originalRequest.headers) {
        return {};
    }

    const reqHeaders = {};
    for (const [key, value] of Object.entries(originalRequest.headers)) {
        reqHeaders[key.toLowerCase()] = value[0].value;
    }

    return reqHeaders;
}

function injectDebugHeader(originalRequest, injectingValue) {
    if (ENABLE_DEBUG_HEADER_INJECTION === true) {
        originalRequest.headers[DEBUG_HEADER_NAME] = [{key: DEBUG_HEADER_NAME, value: injectingValue}];
    }
}

function makeReqCapBody(event) {
    const originalRequest = event.Records[0].cf.request;

    const reqHeaders = readHeadersFromRequest(originalRequest);

    const reqBody = originalRequest.body && originalRequest.body.data;

    let host = reqHeaders['host'];
    const sourceAddress = reqHeaders['x-forwarded-for'] || originalRequest.clientIp || null;
    let body = {
        request: {
            method: originalRequest.method,
            headers: reqHeaders,
            body: reqBody,
            scheme: "https",
            path: originalRequest.uri,
            host: host,
            source_address: sourceAddress,
            source_port: 0
        },
        process_traceable_cookie: ENABLE_CAPTCHA
    };

    return body;
}

function getBrowserIdentifier(originalRequest) {
    const headers = readHeadersFromRequest(originalRequest);
     
    const sourceAddress = headers['x-forwarded-for'] || originalRequest.clientIp || null;
    
    const browserIdentifyingHeaders = {};
    browserIdentifyingHeaders['user-agent'] = headers['user-agent'];
    return  {
        source_address: sourceAddress,
        browser_identifying_headers: browserIdentifyingHeaders
    };

}

function addHeaders(originalRequest, tpaJson){
    const reqHeaderInjections = tpaJson?.decorations?.requestHeaderInjections;
    if(reqHeaderInjections && reqHeaderInjections.length > 0){
        reqHeaderInjections.forEach(injection => {
            if (injection?.key && injection?.value) {
                originalRequest.headers[injection.key] = [{key: injection.key, value: injection.value}];
            }
        });
    }

    if (ADD_TRACE_HEADER === true) {
        let traceParent = tpaJson?.traceContext?.traceparent
        if(traceParent){
            originalRequest.headers[TRACE_HEADER_NAME] = [{key: TRACE_HEADER_NAME, value: traceParent}];
        }
    }
}

function makeHttpCall(options, body) {
    return new Promise((resolve, reject) => {
        const reqBody = body === null ? null : JSON.stringify(body);
        if(reqBody != null){
            options.headers['Content-Length'] = Buffer.byteLength(reqBody);
        }


        const protocol = options.protocol === 'https:' ? https : http;
        if (caBuf.length > 0) {
            options.ca = caBuf;
        }

        const req = protocol.request(options, (res) => {
            if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                // Handle redirection
                const newUrl = new URL(res.headers.location, `${options.protocol}//${options.host}`);
                const newOptions = {
                    ...options,
                    host: newUrl.hostname,
                    path: newUrl.pathname + newUrl.search,
                    protocol: newUrl.protocol,
                };
                makeHttpCall(newOptions, body).then(resolve).catch(reject);
            } else {
                let data = '';
                res.on('data', (chunk) => {
                    data += chunk;
                });

                res.on('end', () => {
                    resolve({
                        statusCode: res.statusCode,
                        body: data,
                    });
                });
            }
        });

        req.on('error', (e) => {
            reject(e);
        });

        req.on('timeout', () => {
            req.abort();
            reject(new Error('Request timed out'));
        });

        req.on('socket', socket => {
            const originalMaxListeners = socket.getMaxListeners();
            if (socket.listenerCount('timeout') >= originalMaxListeners) {
                socket.setMaxListeners(originalMaxListeners + 1);
            }
            const timeoutHandler = () => {
                req.abort();
                reject(new Error('Request timed out'));
            };

            socket.setTimeout(TIMEOUT_MS).once('timeout', timeoutHandler);

            req.on('response', () => {
                socket.removeListener('timeout', timeoutHandler);
            });

            req.on('error', () => {
                socket.removeListener('timeout', timeoutHandler);
            });

            req.on('close', () => {
                socket.removeListener('timeout', timeoutHandler);
            });
        });

        req.setTimeout(TIMEOUT_MS);
        if(reqBody !== null){
            req.write(reqBody);
        }
        req.end();
    });
}

const staticExtensions = [".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".ico", ".woff", ".woff2", ".eot", ".ttf", ".otf", ".webp", ".avif", ".mp4", ".webm"];
function isStatic(uri) {
    return staticExtensions.some(ext => uri.endsWith(ext));
}

function shouldSkip(uri) {
    if (isStatic(uri)) {
        return true;
    }

    // If INCLUDE_PATH_REGEXES_BUILT is not empty, only trace if the path matches a regex in INCLUDE_PATH_REGEXES_BUILT
    if (_INCLUDE_PATH_REGEXES_BUILT.length > 0) {
        return !_INCLUDE_PATH_REGEXES_BUILT.some(regex => regex.test(uri));
    }

    // If INCLUDE_PATH_REGEXES_BUILT is empty and EXCLUDE_PATH_REGEXES_BUILT is not empty, only trace if the path is not excluded
    if (_EXCLUDE_PATH_REGEXES_BUILT.length > 0) {
        return _EXCLUDE_PATH_REGEXES_BUILT.some(regex => regex.test(uri));
    }

    // If neither _INCLUDE_PATH_REGEXES_BUILT nor _EXCLUDE_PATH_REGEXES_BUILT is present, trace everything
    return false;
}

function makeBlockResponse(tpaResponse) {
    let customHeaders = {};
    const resHeaderInjections = tpaResponse?.decorations?.responseHeaderInjections;
    if(resHeaderInjections && resHeaderInjections.length > 0){
        resHeaderInjections.forEach(injection => {
            if (injection?.key && injection?.value) {
                customHeaders[injection.key] = [{key: injection.key, value: injection.value}];
            }
        });
    } else {
        customHeaders['content-type'] = [{key: 'Content-Type', value: 'text/plain'}];
    }
    return {
        status: tpaResponse.statusOnDeny || 403,
        statusDescription: 'Forbidden',
        body: tpaResponse.messageOnDeny || "Access Forbidden",
        headers: customHeaders
    };
}

function makeCaptchaJsResponse(body){
    return {
        status: 200,
        statusDescription: 'OK',
        body: body,
        headers: {
            "content-type": [{key: 'content-type', value: 'application/javascript'}],
        }
    }
}

function makeStepUpResponse(showCaptcha, cookieValues){
    debugLog(cookieValues)
    let headers = {
        'content-type': [{key: 'Content-Type', value: 'application/json'}],
    }

    if(cookieValues.length > 0){
        headers['set-cookie'] = cookieValues;
    }

    return {
        status: 200,
        statusDescription: 'OK',
        body: JSON.stringify({captcha: showCaptcha}),
        headers: headers
    };
}

function debugLog(message) {
    if(ENABLE_DEBUG_LOG === true){
        log(message);
    }
}

function log(message) {
    console.log(TRACEABLE_LOG_PREFIX + message);
}

function readStepUpData(originalRequest) {
    // Body data is b64encoded when we receive it
    // Parse the body data, get the url & token if either present
    let b64BodyData = originalRequest.body && originalRequest.body.data;
    let requestBody = null;
    if(b64BodyData){
        let stringData = Buffer.from(b64BodyData, 'base64').toString()
        requestBody = JSON.parse(stringData)
    }

    let reportedPath = "";
    if (requestBody && requestBody.url) {
        try {
            const urlObj = new URL(requestBody.url);
            reportedPath = urlObj.pathname;
        } catch (e) {
            reportedPath = requestBody.url || "";
        }
    } else {
        log("URL is missing in the requestBody or is invalid:" + requestBody);
    }

    // we return path for regex path check & url for TPA calls
    let data = {
        url: requestBody?.url,
        path: reportedPath,
        token: requestBody?.token,
        browser_identifier: getBrowserIdentifier(originalRequest)
    };
    debugLog("Parsed data from stepUp body");
    debugLog(JSON.stringify(data));
    return data;
}

function addEventName(name, eventsData) {
    if (!name) {
        return;
    }

    if (name.startsWith(CAPTCHA_ERROR_PREFIX)) {
            eventsData.span_name = CAPTCHA_ERROR;
            eventsData.attributes['captcha.error.message'] = name.substring(CAPTCHA_ERROR_PREFIX.length);
    } else {
        eventsData.span_name = name;
    }
}

function readEventsData(originalRequest) {
    const headers = readHeadersFromRequest(originalRequest);
    const sourceAddress = headers['x-forwarded-for'] || originalRequest.clientIp || null;

    // Body data is b64encoded when we receive it
    // Parse the body data, get the url & token if either present
    let b64BodyData = originalRequest.body && originalRequest.body.data;
    let requestBody = null;
    if(b64BodyData){
        let stringData = Buffer.from(b64BodyData, 'base64').toString()
        requestBody = JSON.parse(stringData)
    }

    const attributes = {};
    attributes['net.peer.ip'] = sourceAddress;
    attributes['http.request.header.user-agent'] = headers['user-agent'];
    attributes['page.url'] = requestBody?.url;
    attributes['http.request.header.cookie'] = headers['cookie'];

    let data = {
        attributes: attributes
    };
    addEventName(requestBody?.event, data);

    debugLog("Parsed data from events body: " + JSON.stringify(data));
    return data;
}

function makeEventsResponse() {
    return {
        status: 204,
        statusDescription: 'No Content',
    }
}

function isCaptchaAppliedPath(stepUpData){
    // Check if the path is part of the allowed captcha paths
    if (_CAPTCHA_PATH_REGEXES_BUILT.some(regex => regex.test(stepUpData.path))){
        debugLog(`Path: ${stepUpData.path} is part of captcha regexes`)
        return true
    }
    return false
}

module.exports = {
    handler,
    makeReqCapBody,
    shouldSkip,
    buildRegexs,
    addHeaders,
    debugLog,
    isCaptchaAppliedPath,
    readStepUpData,
    readEventsData,
    readHeadersFromRequest,
    addEventName,
    log,
    injectDebugHeader
};