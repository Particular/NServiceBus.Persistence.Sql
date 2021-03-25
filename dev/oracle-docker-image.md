# Oracle docker image

In order to use Oracle in docker [you will have to create your image](https://github.com/oracle/docker-images/tree/master/OracleDatabase/SingleInstance#running-oracle-database-18c-express-edition-in-a-docker-container).

Here is a summary of the process:

1. Clone <https://github.com/oracle/docker-images/>
2. Download [Oracle Database Express Edition (XE) Release 18.4.0.0.0 (18c)](https://www.oracle.com/database/technologies/xe-downloads.html). On a Mac machine, you can download the Linux package.
3. Move it to `$/OracleDatabase/SingleInstance/dockerfiles/18.4.0`
4. Invoke `$/OracleDatabase/SingleInstance/dockerfiles/buildContainerImage.sh` (git bash or WSL) as `./buildContainerImage.sh -x -v 18.4.0` (build express image)

NOTE: This image is Oracle Database 18c Express Edition. This is different from the one on the build server, which is 11g.
