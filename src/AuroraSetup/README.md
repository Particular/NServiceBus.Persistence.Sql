# AuroraSetup

This project uses the **AWS Cloud Development Kit (CDK)** in C# to provision temporary Aurora MySQL and PostgreSQL clusters for testing and CI scenarios.

## Prerequisites

- [.NET 8+ SDK](https://dotnet.microsoft.com/download)
- [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
- [AWS CDK CLI](https://docs.aws.amazon.com/cdk/latest/guide/getting_started.html)  
  Install with:
  ```
  npm install -g aws-cdk
  ```

## Getting Started

### 1. Install Dependencies

```sh
dotnet restore
```

### 2. Bootstrap Your AWS Environment (first time only)

```sh
cdk bootstrap
```

This sets up resources needed for CDK to deploy stacks.

### 3. Synthesize the CloudFormation Template

Generate the CloudFormation template from your C# code:

```sh
cdk synth
```

### 4. Preview Changes (Diff)

See what changes will be made to your AWS account:

```sh
cdk diff
```

### 5. Deploy the Stack

Deploy the resources to AWS:

```sh
cdk deploy
```

- By default, this creates a VPC, security group, Aurora MySQL and PostgreSQL clusters, and stores generated credentials in AWS Secrets Manager.
- Outputs (such as secret names) will be shown in the terminal and in the CloudFormation console.

### 6. Destroy the Stack

When finished, clean up all resources:

```sh
cdk destroy
```

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