# AuroraSetup

This project uses the **[AWS Cloud Development Kit (CDK)](https://aws.amazon.com/cdk/)** in C# to provision temporary Aurora MySQL and PostgreSQL clusters for testing and CI scenarios (see [`/.github/workflows/aurora.yml`](../../.github/workflows/aurora.yml)).

## Prerequisites

- [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/getting_started.html)  
  Install with:
  ```
  npm install -g aws-cdk
  ```

## Parameter Configuration

This project uses **CDK context parameters** for configuration, such as the stack name.  
You can set these parameters in two ways:

> **Important:** The default stack name `AuroraTestInfrastructure` is reserved for CI. The CI workflow destroys any stack with that name before it deploys. When you deploy into the CI account, pass your own stack name with `-c stackName=<name>`.

### 1. **Edit `cdk.json`**

In the `context` section of your `cdk.json` file, set your desired values:

```json
"context": {
  "stackName": "AuroraTestInfrastructure",
  ...
```

With these set, you can run all CDK commands normally:

```pwsh
cdk synth
cdk diff
cdk deploy
cdk destroy --force
```

### 2. **Pass Context via Command Line**

You can override or provide context values directly on the command line using `-c` (or `--context`):

```pwsh
cdk diff -c stackName=AuroraTest

cdk deploy -c stackName=AuroraTest
```

This is useful for CI, scripting, or temporary overrides.

> **Note:** Command-line context values take precedence over those in `cdk.json`.

## Getting Started

### 1. Install Dependencies

```pwsh
dotnet restore
```

### 2. Bootstrap Your AWS Environment (first time only)

```pwsh
cdk bootstrap
```

This sets up resources needed for CDK to deploy stacks.

### 3. Synthesize the CloudFormation Template

Generate the CloudFormation template from your C# code:

```pwsh
cdk synth
```

### 4. Preview Changes (Diff)

See what changes will be made to your AWS account:

```pwsh
cdk diff
```

### 5. Deploy the Stack

Deploy the resources to AWS:

```pwsh
cdk deploy
```

- By default, this creates a VPC, security group, Aurora MySQL and PostgreSQL clusters, and stores generated credentials in AWS Secrets Manager.
- The stack outputs `MySqlSecretArn` and `PostgreSqlSecretArn` are shown in the terminal and in the CloudFormation console. Use `cdk deploy --outputs-file cdk-outputs.json` to write them to a file.

### 6. Destroy the Stack

When finished, clean up all resources:

```pwsh
cdk destroy --force
```

- The `--force` flag skips the confirmation prompt and immediately deletes all resources in the stack.

## Notes

- **Temporary/Test Use Only:**  
  The stack is configured for easy teardown (`RemovalPolicy.DESTROY`) and uses public subnets and open security groups. **Do not use in production.**
- **Secrets:**  
  Database credentials are generated and stored in AWS Secrets Manager. CloudFormation generates unique secret names, so secrets left behind by an earlier stack do not block a new deployment. Deleting the stack deletes the secrets without a recovery window.
- **Permissions:**  
  The CloudFormation execution role must have permissions to create IAM roles and policies. If you see permission errors, update your IAM policies accordingly.

## Troubleshooting

- **Leftover secrets:**  
  A stack that reaches `DELETE_FAILED` or `ROLLBACK_FAILED` can keep some of its clusters and secrets. Check the stack events in the CloudFormation console to find the resource that failed, fix the cause, and run `cdk destroy --force` again. The CI workflow runs `cdk destroy` before each deploy, so it retries the deletion of a stack that an earlier run left behind. A stack deleted with the option to retain resources leaves those resources in the account; delete them manually.
- **IAM Permission Errors:**  
  Ensure your CDK/CloudFormation execution role has the necessary IAM permissions (see project documentation or ask your AWS admin).