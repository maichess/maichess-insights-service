Feature: Job dispatch
  Submitting an ingestion or analysis records a job and launches the matching
  SparkApplication; the request routes to the right source and job class.

  Scenario: A Lichess ingestion launches an ingestion job
    When an ingestion is submitted for Lichess month "2024-12"
    Then the submit succeeds
    And the job is an ingestion for corpus "lichess-2024-12"
    And a SparkApplication "insights-ingest-job-1" of class "maichess.insights.ingest.IngestJob" was launched
    And the launch argument "--source-type" is "lichess"
    And a submitted event was emitted for the job

  Scenario: An uploaded PGN routes to an upload-source ingestion
    When an ingestion is submitted for uploaded key "uploads/x.pgn"
    Then the submit succeeds
    And the launch argument "--source-type" is "upload"
    And the launch argument "--upload-key" is "uploads/x.pgn"

  Scenario: Analysis over a known corpus launches an analysis job
    Given an ingested corpus "lichess-2024-12"
    When an analysis is submitted for corpus "lichess-2024-12" with kinds "openings,tricky"
    Then the submit succeeds
    And a SparkApplication of class "maichess.insights.analysis.AnalysisJob" was launched
    And the launch argument "--jobs" is "openings,tricky"

  Scenario: Analysis over an unknown corpus is not found
    When an analysis is submitted for corpus "missing" with kinds ""
    Then the submit is not found
    And no SparkApplication was launched
