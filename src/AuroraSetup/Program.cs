using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using AuroraSetup;
using InstanceType = Amazon.CDK.AWS.EC2.InstanceType;

var app = new App();

var stack = new AuroraTestInfrastructure(app, "AuroraTestInfrastructure", new StackProps
{
    // For more information, see https://docs.aws.amazon.com/cdk/latest/guide/environments.html
    Env = new Amazon.CDK.Environment
    {
        Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
        Region = "us-east-2",
    }
});

var vpc = new Vpc(stack, "VPC",
    new VpcProps
    {
        NatGateways = 0,
        SubnetConfiguration = new[]
        {
            new SubnetConfiguration() { Name = "AuroraTestClusterSubnet", SubnetType = SubnetType.PUBLIC }
        }
    });

var securityGroup = new SecurityGroup(stack, "SecurityGroup", new SecurityGroupProps()
{
    AllowAllOutbound = true,
    Vpc = vpc
});

securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(5432), "allow CI for PostgreSQL connections");
securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(3306), "allow CI for MySQL connections");

var postgresCluster = new DatabaseCluster(stack, "PostgreSqlCluster", new DatabaseClusterProps
{
    Engine = DatabaseClusterEngine.AuroraPostgres(new AuroraPostgresClusterEngineProps
    {
        Version = AuroraPostgresEngineVersion.VER_15_2
    }),
    Credentials = Credentials.FromGeneratedSecret("aurora_postgres", new CredentialsBaseOptions { SecretName = "aurora_postgres_secrets" }),
    Vpc = vpc,
    StorageType = DBClusterStorageType.AURORA_IOPT1, // is IO optimized better for tests?
    Writer = ClusterInstance.Provisioned("Writer", new ProvisionedClusterInstanceProps
    {
        InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MEDIUM),
        PubliclyAccessible = true,
    }),
    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
    SecurityGroups = new[]
    {
        securityGroup
    }
});

var mysqlCluster = new DatabaseCluster(stack, "MySqlCluster", new DatabaseClusterProps
{
    Engine = DatabaseClusterEngine.AuroraMysql(new AuroraMysqlClusterEngineProps
    {
        Version = AuroraMysqlEngineVersion.VER_3_03_0
    }),
    Credentials = Credentials.FromGeneratedSecret("aurora_mysql", new CredentialsBaseOptions { SecretName = "aurora_mysql_secrets" }),
    Vpc = vpc,
    StorageType = DBClusterStorageType.AURORA,
    Writer = ClusterInstance.Provisioned("Writer", new ProvisionedClusterInstanceProps
    {
        InstanceType = InstanceType.Of(InstanceClass.T3, InstanceSize.MEDIUM),
        PubliclyAccessible = true
    }),
    VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
    SecurityGroups = new[]
    {
        securityGroup
    }
});

_ = new CfnOutput(stack, "postgres_secrets", new CfnOutputProps { Value = postgresCluster.Secret!.SecretName });
_ = new CfnOutput(stack, "mysql_secrets", new CfnOutputProps { Value = mysqlCluster.Secret!.SecretName });

app.Synth();