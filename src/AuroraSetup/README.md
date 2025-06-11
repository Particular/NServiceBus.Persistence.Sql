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

This project uses **CDK context parameters** for configuration, such as stack name and secret names.  
You can set these parameters in two ways:

### 1. **Edit `cdk.json`**

In the `context` section of your `cdk.json` file, set your desired values:

```json
"context": {
  "stackName": "AuroraTestInfrastructure",
  "mysqlSecretName": "aurora_mysql_secrets",
  "postgresSecretName": "aurora_postgres_secrets"
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
cdk diff -c stackName=AuroraTest -c mysqlSecretName=aurora_test_mysql_secret -c postgresSecretName=aurora_test_postgres_secret

cdk deploy -c stackName=AuroraTest -c mysqlSecretName=aurora_test_mysql_secret -c postgresSecretName=aurora_test_postgres_secret
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
- Outputs (such as secret names) will be shown in the terminal and in the CloudFormation console.

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
  Database credentials are generated and stored in AWS Secrets Manager. Secret names are output after deployment.
- **Permissions:**  
  The CloudFormation execution role must have permissions to create IAM roles and policies. If you see permission errors, update your IAM policies accordingly.

## Troubleshooting

- **Secret Already Exists:**  
  If you see errors about secrets already existing, use unique secret names or delete the old secrets from AWS Secrets Manager.
- **IAM Permission Errors:**  
  Ensure your CDK/CloudFormation execution role has the necessary IAM permissions (see project documentation or ask your AWS admin).