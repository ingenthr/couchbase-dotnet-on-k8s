Couchbase Stellar Gateway on Kubernetes
=======================================

The motivation for this was a need to test a DNS SRV lookup scenario
under Kubernetes, but this should be reusable in general

Steps
-----
1. Get KIND, Kubernetes in Docker
2. Create a secret with your Github Personal Access token, see below.
3. Create the secret for appdemo `kubectl apply -f appdemo-secret.yaml` from the k8s/kube-deployment directory
4. Set up a K8S cluster.  I used 1.25, specified from the config.  From the k8s directory in this repo, `kind create cluster --config kind-config.yaml --name appdemo`.  This will automatically put it into your kubectl context which you can verify with `kubectl config current-context`.
5. Deploy Couchbase Operator/DAC.  From the k8s/kube-deployment directory… `kubectl apply -f couchbase-crd.yaml` then the same for the secrets, then the same for the `cb-operator-dac-deployment.yaml`
6. Deploy Couchbase Server: `kubectl apply -f cbcluster-buckets-users.yaml`.  Wait a while.

You can check for status with `kubectl get pods` and you'll eventually see something like this with everything "READY":
```
% kubectl get pods
NAME                                            READY   STATUS    RESTARTS      AGE
cb-appdemo-0000                                 2/2     Running   0             73m
cb-appdemo-0001                                 2/2     Running   0             73m
cb-appdemo-0002                                 2/2     Running   0             72m
couchbase-operator-69549b95db-2brjj             1/1     Running   5 (75m ago)   4d5h
couchbase-operator-admission-86f5cf7446-7hxt4   1/1     Running   1 (76m ago)   4d5h
```


Do note that the above steps require a little bit of time between each one.  Probably just enough to copy and paste, but as an example the DAC needs to be deployed before creating a cluster.  The last step does take some time.  The commands `kubectl get pods` and `kubectl logs <pod> <container>` are your friends.  As an exmaple, you should be able to do steps 1-3 and check the pods with get pods.  Then do step 4 and watch the operator's log as it creates the cluster, networks it and so on.

Verifying
---------

As a quick test, forward a port:
`kubectl port-forward service/cb-appdemo-endpoint-proxy-service 8080:80`

Then call it with `grpcurl` against a default Health/Check once you have the definition.

To get the definition: `curl -o health.proto https://raw.githubusercontent.com/grpc/grpc-proto/master/grpc/health/v1/health.proto`

Then invoke with:
`grpcurl -plaintext -proto health.proto -d '{ "service": "hello" }' localhost:8080 grpc.health.v1.Health/Check`

```
% grpcurl -plaintext -proto health.proto -d '{ "service": "hello" }' localhost:8080 grpc.health.v1.Health/Check
{
  "status": "SERVING"
}
```

Creating a Secret to get Containers
-----------------------------------
Create a personal access token with read:packages scope at https://github.com/settings/tokens/new, then

`kubectl create secret docker-registry ghcr-login-secret --docker-server=https://ghcr.io --docker-username=$YOUR_GITHUB_USERNAME --docker-password=$YOUR_GITHUB_TOKEN --docker-email=$YOUR_EMAIL`


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

Troubleshooting
---------------

To diagnose image pull issues…
`kubectl describe pod <podname>`

TODO
----
- Regenerate with image pull secrets as an argument to `cao generate`.  Should put it in a better place.
- Add an ingress.


Issue related
https://github.com/MichaCo/DnsClient.NET/issues/4#issuecomment-819805640