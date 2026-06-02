Feature: Secret endpoint
    The /Secret endpoint exposes secrets configured via appsettings.

Scenario: Returns the configured secrets
    When I send a GET request to "/Secret"
    Then the response status code is 200
    And the response is a non-empty JSON string array
