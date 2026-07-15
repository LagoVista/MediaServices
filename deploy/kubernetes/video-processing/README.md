# Video Processing Kubernetes Resources

The video processor runs one request per Kubernetes Job. The main portal API creates each Job dynamically and passes the frozen request SAS URL through the worker command line.

## Installed once

Apply the reusable namespace resources with:

    kubectl apply -k deploy/kubernetes/video-processing

Create environment-specific copies of the templates, replace their placeholder values, and apply them with:

    kubectl apply -f deploy/kubernetes/video-processing/secret.yaml
    kubectl apply -f deploy/kubernetes/video-processing/launcher-role-binding.yaml

Do not commit populated secrets.

## Main portal API permissions

launcher-role-binding.template.yaml binds the Role in the video-processing namespace to the ServiceAccount used by the main portal API deployment.

Replace these values:

- replace-with-main-api-service-account
- replace-with-main-api-namespace

The main API may then create, inspect, watch, and delete video processor Jobs. The worker ServiceAccount has no Kubernetes API permissions and does not receive an API token.

## Per-attempt Jobs

job-template.yaml is an example, not a permanently installed resource. The main API should construct a Job for every processing attempt with unique values for:

- Job name
- Request ID
- Attempt ID
- Production ID
- Job type
- Worker image version
- Request read SAS URL

Use backoffLimit 0 and restartPolicy Never. Application-level retries should create a new attempt and a new Job rather than silently rerunning the same attempt.

## Workspace lifecycle

The worker writes temporary media to /workspace, backed by a size-limited emptyDir. Kubernetes removes this storage when the Pod is deleted. Only outputs deliberately uploaded to Azure or Vimeo survive the Job.

## Future Helm migration

These files intentionally keep values visible and repetitive so they can later map cleanly into a Helm chart:

- Namespace
- Image repository and tag
- Main API ServiceAccount and namespace
- Callback service URL
- RabbitMQ connection settings
- Requests and limits
- Ephemeral workspace size
- Active deadline
- Finished-job TTL
