Feature: Random endpoint
    The /Random endpoint returns random strings within configured bounds.

Scenario: Returns between 2 and 10 random strings of length 2 to 10
    When I send a GET request to "/Random"
    Then the response status code is 200
    And the response is a non-empty JSON string array
    And the JSON array has between 2 and 10 items
    And each string in the JSON array has length between 2 and 10
