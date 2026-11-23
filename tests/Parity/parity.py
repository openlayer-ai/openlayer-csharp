"""Python-SDK side of the parity harness. Mirrors tests/Parity/Program.cs call for call."""
import json
import os
import time

from openlayer import APIStatusError, Openlayer

D = "182bd5e5-6e1a-4fe4-a799-aa6d9a6ab26e"
project_id = os.environ.get("PROJECT_ID", D)
pipeline_id = os.environ.get("PIPELINE_ID", D)
workspace_id = os.environ.get("WORKSPACE_ID", D)
project_name = os.environ.get("PROJECT_NAME", "csharp-parity")
# scripts/parity sets one TIMESTAMP for both SDKs; the backend rejects rows older than 2 years.
timestamp = int(os.environ.get("TIMESTAMP") or time.time())

c = Openlayer(api_key=os.environ.get("OPENLAYER_API_KEY", "test-key"), max_retries=0)
out = {}


def run(name, f):
    try:
        out[name] = f()
    except APIStatusError as e:
        out[name] = f"ERROR {e.status_code}"
    except Exception as e:  # noqa: BLE001
        out[name] = f"ERROR {type(e).__name__}"


def create():
    r = c.projects.create(name=project_name, task_type="llm-base", description="SDK parity test")
    return "created" if r.name == project_name else r.name


run("projects.create", create)
run("projects.list", lambda: sorted(p.name for p in c.projects.list(per_page=100, name=project_name).items))
run("projects.inference_pipelines.list", lambda: sorted(p.name for p in c.projects.inference_pipelines.list(project_id).items))
run("inference_pipelines.retrieve", lambda: c.inference_pipelines.retrieve(pipeline_id).name)
run("workspaces.retrieve", lambda: c.workspaces.retrieve(workspace_id).name)
run(
    "inference_pipelines.data.stream",
    lambda: c.inference_pipelines.data.stream(
        pipeline_id,
        rows=[{"user_query": "what is the meaning of life?", "output": "42", "tokens": 7, "cost": 0.02, "timestamp": timestamp}],
        config={
            "input_variable_names": ["user_query"],
            "output_column_name": "output",
            "num_of_token_column_name": "tokens",
            "cost_column_name": "cost",
            "timestamp_column_name": "timestamp",
        },
    ).success,
)
print(json.dumps(dict(sorted(out.items())), indent=2))
