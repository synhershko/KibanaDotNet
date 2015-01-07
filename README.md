Kibana 4 host for the .NET platform and Azure
===========================================

Making running and hosting of Kibana on Windows platform easier 

## How to run

1. Have Elasticsearch running. To run locally:
	1. Download from http://elasticsearch.org/download
	2. Go edit config\elasticsearch.yml and edit:
		* `cluster.name` to something non-default. Your GitHub username will do.
		* replica and shard count, by specifing `index.number_of_shards: 1` and `index.number_of_replicas: 0` (unless you know what you are doing)
	3. Make sure you have JAVA_HOME properly set up
	4. Run `elasticsearch\bin\elasticsearch.bat`
2. Compile and run KibanaHost
3. Go to http://localhost:5601/ to view Kibana

On first launch KibanaHost will download the latest version of Kibana 4 automatically. You can change various configurations by editing the kibana.yml which will appear next to your application executable. Don't forget to restart, tho.

## Azure instructions

TBD
