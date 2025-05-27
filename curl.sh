#!/bin/bash

# Number of iterations (change as needed)
NUM_REQUESTS=10

# URL
URL="http://localhost:5001/echo"

# Headers
HEADERS=(
  "-H" "x-forwarded-for: 117.215.190.191"
  "-H" "jacob-process: process_me"
  "-H" "Content-Type: application/json"
)

# JSON payload
DATA='{
    "data": [
        {
            "name": "Maci",
            "telephone": 33966454,
            "curr_city": "Madrid",
            "weather": "Freezing Fog in Vicinity",
            "nested": [
                {
                    "name": "Dorothy",
                    "telephone": 54709803,
                    "curr_city": "Las Vegas",
                    "weather": "Heavy Rain"
                }
            ]
        }
    ],
    "authorization": "traceable-auth-val",
    "additional": "temptemptemp",
    "num1": 1,
    "num2": 2
}'

# Loop
for ((i=1; i<=NUM_REQUESTS; i++))
do
    echo "Request #$i"
    curl --location "${URL}/${i}" "${HEADERS[@]}" --data "$DATA"
    echo -e "\n----------------------"
done

