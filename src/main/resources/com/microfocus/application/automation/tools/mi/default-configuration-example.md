# Autonomous Tester default configuration

## Editing the configuration

Edit the adjacent `default-configuration.json` on the Jenkins controller:

```text
<JENKINS_HOME>\aute-config\<configurationId>\<spaceId>\
```

AuTe jobs for this connection and shared space use this configuration. Each build
copies and archives a snapshot; edits affect only later snapshots. A build fails
when the file is not a valid JSON object.

The top-level blocks are `BROWSER`, `AGENT`, `LLM_MODELS`, and `ENVIRONMENT`, each a
JSON object. Only `LLM_MODELS` is required; the build fails before any run when it is
missing or not a JSON object. Use valid JSON: double-quoted names and strings,
unquoted numbers, booleans, and `null`, with no comments or trailing commas.
`LLM_ENDPOINT` and `APLS_LICENSE` do not belong in this file; Jenkins adds them
from credentials (see below).

Values below are examples, not defaults. Check your mi-agent version for required
fields, supported values, timeout units, and how omitted values, `null`, and empty
lists behave.

**Sensitive data:** values in this archived JSON are not protected as Jenkins
Credentials. Restrict file and artifact access. Configure LLM endpoint and APLS
license secrets through Jenkins Credentials, not here.

## Credentials (LLM endpoint and APLS license)

**Where to add them:** Jenkins creates each new AuTe job in the folder
*AuTe - shared space &lt;spaceId&gt;* (item name `AuTe-<configurationId>-<spaceId>`,
inside `UFT_TEST_RUNNER_FOLDER` when set). Add the credentials to that folder:
*folder → Credentials → Folder → Global → Add Credentials*. These credential types
are available only in folder credential stores, not in *Manage Jenkins → Credentials*.
Jenkins searches the job's folder and then its parent folders, nearest first;
global and user credentials are ignored.

| Credential kind                                       | Selected by                                                                                                                                                               | Sent to mi-agent as |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------- |
| *Autonomous Tester LLM JSON Configuration*          | The run's `ENDPOINT_PARAM_NAME` from Software Delivery Management; if empty, *Endpoint parameter name* in *Manage Jenkins → System → Autonomous Tester settings*. | `LLM_ENDPOINT`    |
| *Autonomous Tester APLS License JSON Configuration* | *APLS license parameter name* in *Manage Jenkins → System → Autonomous Tester settings*.                                                                            | `APLS_LICENSE`    |

Both kinds have the same fields: **ID** (the name referenced above),
**Description**, and **Configuration JSON** (masked). Paste the whole JSON object
into **Configuration JSON**. A run fails when no name is set, no folder credential
with that ID exists, or the value is not a JSON object.

## BROWSER

Controls the browser used for execution.

| Setting                     | JSON type        | Purpose                                                                         |
| --------------------------- | ---------------- | ------------------------------------------------------------------------------- |
| `BROWSER_NAME`            | string           | Browser to use.                                                                 |
| `BROWSER_LOCALE`          | string           | Browser locale, for example `en-US`.                                          |
| `BROWSER_CHANNEL`         | string or null   | Browser distribution/channel.                                                   |
| `BROWSER_EXECUTABLE_PATH` | string or null   | Executable on the agent; escape backslashes, e.g. `C:\\Browsers\\chrome.exe`. |
| `ALLOWED_DOMAINS`         | array of strings | Allowed domains; verify format and empty-list behavior.                         |
| `PROXY_SERVER`            | string or null   | Browser proxy server.                                                           |

```json
{
  "BROWSER": {
    "BROWSER_NAME": "chrome",
    "BROWSER_LOCALE": "en-US",
    "BROWSER_CHANNEL": null,
    "BROWSER_EXECUTABLE_PATH": null,
    "ALLOWED_DOMAINS": [],
    "PROXY_SERVER": null
  }
}
```

## AGENT

Sets execution limits as integers. Check runner-supported ranges and timeout units.

| Setting                     | JSON type | Purpose                  |
| --------------------------- | --------- | ------------------------ |
| `MAX_FAILURES`            | integer   | Failure limit.           |
| `MAX_NUMBER_OF_STEPS`     | integer   | Maximum execution steps. |
| `BROWSER_USE_RUN_TIMEOUT` | integer   | Browser-use run timeout. |

```json
{
  "AGENT": {
    "MAX_FAILURES": 3,
    "MAX_NUMBER_OF_STEPS": 100,
    "BROWSER_USE_RUN_TIMEOUT": 100000
  }
}
```

## LLM_MODELS

Sets execution and analysis models. Choose model names, thinking levels, and
temperatures supported by your LLM endpoint.

| Block               | Setting                         | JSON type | Purpose                                 |
| ------------------- | ------------------------------- | --------- | --------------------------------------- |
| `EXECUTION_MODEL` | `LLM_EXECUTOR_MODEL`          | string    | Model used for execution.               |
| `EXECUTION_MODEL` | `LLM_EXECUTOR_THINKING_LEVEL` | string    | Execution model's reasoning level.      |
| `EXECUTION_MODEL` | `LLM_EXECUTOR_TEMPERATURE`    | number    | Execution model's sampling temperature. |
| `ANALYSIS_MODEL`  | `LLM_ANALYZER_MODEL`          | string    | Model used for analysis.                |
| `ANALYSIS_MODEL`  | `LLM_ANALYZER_THINKING_LEVEL` | string    | Analysis model's reasoning level.       |
| `ANALYSIS_MODEL`  | `LLM_ANALYZER_TEMPERATURE`    | number    | Analysis model's sampling temperature.  |

```json
{
  "LLM_MODELS": {
    "EXECUTION_MODEL": {
      "LLM_EXECUTOR_MODEL": "gemini-3.8-flash",
      "LLM_EXECUTOR_THINKING_LEVEL": "medium",
      "LLM_EXECUTOR_TEMPERATURE": 0.2
    },
    "ANALYSIS_MODEL": {
      "LLM_ANALYZER_MODEL": "gemini-3.8-flash",
      "LLM_ANALYZER_THINKING_LEVEL": "high",
      "LLM_ANALYZER_TEMPERATURE": 0.2
    }
  }
}
```

## APLS_LICENSE (Jenkins credential, not this file)

`APLS_LICENSE` is not part of this file. Store it as an *Autonomous Tester APLS
License JSON Configuration* credential in the AuTe job folder, and set its ID in
*Manage Jenkins → System → Autonomous Tester settings → APLS license parameter name*.
Every run requires it.

If an older copy of this file still has an `APLS_LICENSE` block, the credential
replaces it at run time, but the block is still archived with every build. Remove it.

The credential's **Configuration JSON** sets AutoPass connection and authentication
details. Ask your license administrator which authentication fields your deployment needs.

| Setting                        | JSON type      | Purpose                                  |
| ------------------------------ | -------------- | ---------------------------------------- |
| `AUTOPASS_HOST`              | string or null | AutoPass host reachable from the agent.  |
| `AUTOPASS_PORT`              | integer        | AutoPass port, e.g. `5814`.            |
| `AUTOPASS_USERNAME`          | string or null | Authentication username, if required.    |
| `AUTOPASS_PASSWORD`          | string or null | Authentication password (sensitive).     |
| `AUTOPASS_SERVER_AUTH_TOKEN` | string or null | Server authentication token (sensitive). |
| `AUTOPASS_TOKEN`             | string or null | Licensing token (sensitive).             |
| `AUTOPASS_APAUTHTOKEN`       | string or null | AP authentication token (sensitive).     |

Example **Configuration JSON** value:

```json
{
  "AUTOPASS_HOST": null,
  "AUTOPASS_PORT": 5814,
  "AUTOPASS_USERNAME": null,
  "AUTOPASS_PASSWORD": null,
  "AUTOPASS_SERVER_AUTH_TOKEN": null,
  "AUTOPASS_TOKEN": null,
  "AUTOPASS_APAUTHTOKEN": null
}
```

## ENVIRONMENT

Controls execution features. This block is optional; when it is missing, Jenkins
creates it for each run with the values below.

Jenkins sets these values for every run and overwrites anything set here:

| Value                                       | Set to                                                         |
| ------------------------------------------- | -------------------------------------------------------------- |
| `ENVIRONMENT.OUTPUT_DIR`                  | The run's result folder in the build workspace.                |
| `ENVIRONMENT.EXECUTION_RECORDING_ENABLED` | `false` (execution recording is not supported from Jenkins). |
| `LLM_ENDPOINT`                            | The selected LLM credential's JSON.                            |
| `APLS_LICENSE`                            | The selected APLS license credential's JSON.                   |

## Complete example

Copy and adapt this configuration:

```json
{
  "BROWSER": {
    "BROWSER_NAME": "chrome",
    "BROWSER_LOCALE": "en-US",
    "BROWSER_CHANNEL": null,
    "BROWSER_EXECUTABLE_PATH": null,
    "ALLOWED_DOMAINS": [],
    "PROXY_SERVER": null
  },
  "AGENT": {
    "MAX_FAILURES": 3,
    "MAX_NUMBER_OF_STEPS": 100,
    "BROWSER_USE_RUN_TIMEOUT": 100000
  },
  "LLM_MODELS": {
    "EXECUTION_MODEL": {
      "LLM_EXECUTOR_MODEL": "gemini-3.8-flash",
      "LLM_EXECUTOR_THINKING_LEVEL": "medium",
      "LLM_EXECUTOR_TEMPERATURE": 0.2
    },
    "ANALYSIS_MODEL": {
      "LLM_ANALYZER_MODEL": "gemini-3.8-flash",
      "LLM_ANALYZER_THINKING_LEVEL": "high",
      "LLM_ANALYZER_TEMPERATURE": 0.2
    }
  }
}
```

## File preservation and guide updates

Existing files are never overwritten. Missing files are added during AuTe job
creation or a build's configuration snapshot: a missing `default-configuration.json`
is recreated as an empty template with the four blocks, and a missing guide is
restored from the bundled version. To refresh the guide after an upgrade, delete
only `default-configuration-example.md`.
