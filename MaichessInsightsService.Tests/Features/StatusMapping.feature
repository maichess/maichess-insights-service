Feature: Spark status mapping
  The control plane maps the Spark Operator's applicationState to a job status;
  unrecognized or non-terminal states leave a tracked job unchanged.

  Scenario Outline: Operator states map to job status
    Given a job currently "<current>"
    When the SparkApplication reports state "<state>"
    Then the mapped status is "<mapped>"

    Examples:
      | current | state             | mapped    |
      | pending | RUNNING           | running   |
      | pending | SUCCEEDING        | running   |
      | running | COMPLETED         | succeeded |
      | running | FAILED            | failed    |
      | running | FAILING           | failed    |
      | pending | SUBMISSION_FAILED | failed    |
      | pending | SUBMITTED         | pending   |
      | running |                   | running   |
