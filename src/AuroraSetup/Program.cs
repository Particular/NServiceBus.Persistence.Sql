﻿using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using AuroraSetup;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

var app = new App();

var stackName = app.Node.TryGetContext("stackName") as string;
ArgumentException.ThrowIfNullOrEmpty(stackName, nameof(stackName));

var mysqlSecretName = app.Node.TryGetContext("mysqlSecretName") as string;
ArgumentException.ThrowIfNullOrEmpty(mysqlSecretName, nameof(mysqlSecretName));

var postgresSecretName = app.Node.TryGetContext("postgresSecretName") as string;
ArgumentException.ThrowIfNullOrEmpty(postgresSecretName, nameof(postgresSecretName));

var stack = new AuroraTestInfrastructure(app, stackName, new StackProps
{
    // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = "us-east-2",
    },
    Synthesizer = new DefaultStackSynthesizer(new DefaultStackSynthesizerProps
    {
        // There is a dedicated CDK toolkit (including CF service roles) stack with reduced permissions
        Qualifier = "aurora-ci"
    })
});

var vpc = new Vpc(stack, "VPC",
    new VpcProps
    {
        NatGateways = 0,
        SubnetConfiguration =
        [
            new SubnetConfiguration() { Name = "AuroraTestClusterSubnet", SubnetType = SubnetType.PUBLIC }
        ]
    });

var securityGroup = new SecurityGroup(stack, "SecurityGroup", new SecurityGroupProps()
{
    AllowAllOutbound = true,
    Vpc = vpc
});

securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(5432), "allow CI for PostgreSQL connections");
securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(3306), "allow CI for MySQL connections");

var mysqlCluster = new DatabaseCluster(stack, "MySqlCluster", new DatabaseClusterProps
{
    DeletionProtection = false,
    Engine = DatabaseClusterEngine.AuroraMysql(new AuroraMysqlClusterEngineProps
    {
        Version = AuroraMysqlEngineVersion.VER_3_09_0
    }),
    Credentials = Credentials.FromGeneratedSecret("aurora_mysql", new CredentialsBaseOptions { SecretName = mysqlSecretName }),
    Vpc = vpc,
    StorageType = DBClusterStorageType.AURORA,
    Writer = ClusterInstance.Provisioned("Writer", new ProvisionedClusterInstanceProps
    {
        InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MEDIUM),
        PubliclyAccessible = true
    }),
    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
    SecurityGroups = [securityGroup],
    RemovalPolicy = RemovalPolicy.DESTROY
});

var postgresCluster = new DatabaseCluster(stack, "PostgreSqlCluster", new DatabaseClusterProps
{
    DeletionProtection = false,
    Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
    {
        Version = AuroraPostgresEngineVersion.VER_17_4
    }),
    Credentials = Credentials.FromGeneratedSecret("aurora_postgres", new CredentialsBaseOptions { SecretName = postgresSecretName }),
    Vpc = vpc,
    StorageType = DBClusterStorageType.AURORA_IOPT1, // is IO optimized better for tests?
    Writer = ClusterInstance.Provisioned("Writer", new ProvisionedClusterInstanceProps
    {
        InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MEDIUM),
        PubliclyAccessible = true,
    }),
    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
    SecurityGroups = [securityGroup],
    RemovalPolicy = RemovalPolicy.DESTROY
});

_ = new CfnOutput(stack, "postgres_secrets", new CfnOutputProps { Value = postgresCluster.Secret!.SecretName });
_ = new CfnOutput(stack, "mysql_secrets", new CfnOutputProps { Value = mysqlCluster.Secret!.SecretName });

app.Synth();
