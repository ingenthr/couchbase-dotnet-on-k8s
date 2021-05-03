Generic .NET Core 5 App for Couchbase on Kubernetes
===================================================

The motivation for this was a need to test a DNS SRV lookup scenario
under Kubernetes, but this should be reusable in general

Steps
-----
1. Get KIND, Kubernetes in Docker
2. Set up a K8S cluster.  I used 1.20.  From the k8s directory in this repo, `kind create cluster --config kind-config.yaml --name appdemo`
3. Deploy Couchbase Operator/DAC.  From the k8s/kube-deployment directory… `kubectl apply -f couchbase-crd.yaml` then the same for the secrets, then the same for the `cb-operator-dac-deployment.yaml` 
4. Deploy Couchbase Server.  `kubectl apply -f cb-cluster-buckets-users.yaml`
5. Build the .NET app from the dockerfile and push it to KIND.  From the root…  `docker build -t dotnetappdemo:0.4 .` and `kind load --name appdemo docker-image dotnetappdemo:0.4`
6. Deploy the app and look at the log output: `kubectl apply -f dotnetexample.yaml` and `kubectl logs -f service/dotnetexample`

Do note that the above steps require a little bit of time between each one.  Probably just enough to copy and paste, but as an example the DAC needs to be deployed before creating a cluster.  

ToDo
----
1. Properly use secrets from the app
2. Set up a way to attach a debugger
3. Have a better app that doens't exit and maybe sticks around with HTTP endpoints for making it do things.
4. Prometheus.

Misc. Notes
-----------

To check the DNS SRV records, the [DNS utils pod](https://kubernetes.io/docs/tasks/administer-cluster/dns-debugging-resolution/) may be deployed.  Then you can check like so:

```
kube-deployment % kubectl exec -ti dnsutils -- nslookup -q=SRV _couchbase._tcp.cb-appdemo.default.svc               
Server:         10.96.0.10
Address:        10.96.0.10#53

_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0000.cb-appdemo.default.svc.cluster.local.
_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0001.cb-appdemo.default.svc.cluster.local.
_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0002.cb-appdemo.default.svc.cluster.local.
```

Issue related
https://github.com/MichaCo/DnsClient.NET/issues/4#issuecomment-819805640
