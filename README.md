# Couchbase Stellar Gateway on Kubernetes

The motivation for this was a need to test a DNS SRV lookup scenario
under Kubernetes, but this should be reusable in general and has been
reused here to demonstrate running under OpenShift.

## Steps

Before we get started, a side note to the unitiatied-- OpenShift uses a command `oc` which is like `kubectl` but (ahem), extended. You may see both here since this was derived from Kubernetes based docs.

1. Get an OpenShift Cluster. If you don't have 7 computers laying around and access to RH Downloads, you can use [ROSA](https://access.redhat.com/documentation/en-us/red_hat_openshift_service_on_aws). This takes about 2 hours to go through the first time if you follow the fast path, but subsequent starts only take 5-6 commands and 40 minutes or so.
2. Create a secret with your Github Personal Access token, see below. You will need this in order to access the private repositories.
3. Create a Route.  Technically, it won't work at all yet, but you will need to know the hostname (derived from ROSA) in order to add the subject-alternate-names to the TLS secrets.  This can be done with ` % oc create route passthrough --service cb-appdemo-cloud-native-gateway-service` and then get the route name from the output in `oc get routes`.
4. Create TLS secrets needed for the cluster. This uses a passthrough Route, meaning the CouchbaseCluster will be configured with the TLS secrets that are used externally, so it will require a Subject Alternate Name that matches the domain name.  Follow the tutorial if you want to do this with your own CA via easyrsa.  A sample invocation of easrsa with the SANs is below.
5. Create the secret for appdemo `oc apply -f appdemo-secret.yaml`, `oc apply -f cb-appdemo-admin-secret.yaml`, `oc apply -f ghcr-login-secret.yaml` (which you created from step 3), from the k8s/kube-deployment directory. Then deploy the TLS certs (which you created from step 2) with something like `oc apply -f couchbase-server-ca.yaml`, `oc apply -f couchbase-server-tls.yaml`.
6. Deploy Couchbase Operator/DAC. From the k8s/kube-deployment directory… `oc apply -f couchbase-crd.yaml` then the same for the secrets, then the same for the `cb-operator-dac-deployment.yaml`
7. Deploy Couchbase Server: `oc apply -f cb-cluster-buckets-users.yaml`. Wait a while.

You can check for status with `oc get pods` and you'll eventually see something like this with everything "READY":

```
% kubectl get pods
NAME                                            READY   STATUS    RESTARTS      AGE
cb-appdemo-0000                                 2/2     Running   0             73m
cb-appdemo-0001                                 2/2     Running   0             73m
cb-appdemo-0002                                 2/2     Running   0             72m
couchbase-operator-69549b95db-2brjj             1/1     Running   5 (75m ago)   4d5h
couchbase-operator-admission-86f5cf7446-7hxt4   1/1     Running   1 (76m ago)   4d5h
```

And you may see the services, in particular the gateway (previously endpoint-proxy):

```
% oc get services
NAME                                      TYPE           CLUSTER-IP       EXTERNAL-IP                            PORT(S)                                                                                                                                                                                                                                                                                                                                                                               AGE
cb-appdemo                                ClusterIP      None             <none>                                 4369/TCP,8091/TCP,8092/TCP,8093/TCP,8094/TCP,8095/TCP,8096/TCP,9100/TCP,9101/TCP,9102/TCP,9103/TCP,9104/TCP,9105/TCP,9110/TCP,9111/TCP,9112/TCP,9113/TCP,9114/TCP,9115/TCP,9116/TCP,9117/TCP,9118/TCP,9120/TCP,9121/TCP,9122/TCP,9130/TCP,9140/TCP,9999/TCP,11207/TCP,11209/TCP,11210/TCP,18091/TCP,18092/TCP,18093/TCP,18094/TCP,18095/TCP,18096/TCP,19130/TCP,21100/TCP,21150/TCP   3m15s
cb-appdemo-cloud-native-gateway-service   ClusterIP      172.30.174.87    <none>                                 443/TCP                                                                                                                                                                                                                                                                                                                                                                               2m7s
cb-appdemo-srv                            ClusterIP      None             <none>                                 11210/TCP,11207/TCP
```

Do note that the above steps require a little bit of time between each one. Probably just enough to copy and paste, but as an example the DAC needs to be deployed before creating a cluster. The last step does take some time. The commands `kubectl get pods` and `kubectl logs <pod> <container>` are your friends. As an exmaple, you should be able to do steps 1-3 and check the pods with get pods. Then do step 4 and watch the operator's log as it creates the cluster, networks it and so on.

Add a certificate. This command aligns to the Operator documentation tutorial around using easyrsa as your CA. Public CA would, of course, be different. Note that the last two arguments in the subject-alt-name _will be specific_ to your particular deployment. You may wish to create a route to a simple service first, any will do, to see what the domain will be.

```
% easyrsa --subject-alt-name='DNS:*.cb-appdemo,DNS:*.cb-appdemo.default,DNS:*.cb-appdemo.default.svc,DNS:*.cb-appdemo.default.svc.cluster.local,DNS:cb-appdemo-srv,DNS:cb-appdemo-srv.default,DNS:cb-appdemo-srv.default.svc,DNS:*.cb-appdemo-srv.default.svc.cluster.local,DNS:localhost,DNS:*.apps.cngateway-demo.fg1b.p1.openshiftapps.com,DNS:cb-appdemo-cloud-native-gateway-service-default.apps.cngateway-demo.fg1b.p1.openshiftapps.com' build-server-full cb-appdemo-cloud-native-gateway-service nopass
```

## Verifying

As a quick test, forward a port:
`kubectl port-forward service/cb-appdemo-endpoint-proxy-service 8443:443`

Then call it with `grpcurl` against a default Health/Check once you have the definition.

To get the definition: `curl -o health.proto https://raw.githubusercontent.com/grpc/grpc-proto/master/grpc/health/v1/health.proto`

Then invoke with:
`grpcurl --insecure -proto health.proto -d '{ "service": "hello" }' localhost:8443 grpc.health.v1.Health/Check`

```
% grpcurl --insecure -proto health.proto -d '{ "service": "hello" }' localhost:8443 grpc.health.v1.Health/Check
{
  "status": "SERVING"
}
```

## Creating a Secret to get Containers

Create a 'classic' personal access token with read:packages scope at https://github.com/settings/tokens/new, then

`kubectl create secret docker-registry ghcr-login-secret --docker-server=https://ghcr.io --docker-username=$YOUR_GITHUB_USERNAME --docker-password=$YOUR_GITHUB_TOKEN --docker-email=$YOUR_EMAIL`

## Misc. Notes

To check the DNS SRV records, the [DNS utils pod](https://kubernetes.io/docs/tasks/administer-cluster/dns-debugging-resolution/) may be deployed. Then you can check like so:

```
kube-deployment % kubectl exec -ti dnsutils -- nslookup -q=SRV _couchbase._tcp.cb-appdemo.default.svc
Server:         10.96.0.10
Address:        10.96.0.10#53

_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0000.cb-appdemo.default.svc.cluster.local.
_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0001.cb-appdemo.default.svc.cluster.local.
_couchbase._tcp.cb-appdemo.default.svc.cluster.local    service = 0 33 11210 cb-appdemo-0002.cb-appdemo.default.svc.cluster.local.
```

## Troubleshooting

To diagnose image pull issues…
`kubectl describe pod <podname>`

To see what the operator is doing if something seems 'stuck':
`% oc logs deployment/couchbase-operator`
… and add a -f to follow along and watch.


To diagnose what cert is presented by the endpoint (assuming port forward):
```
% openssl s_client -showcerts \
-servername cb-appdemo-cloud-native-gateway-service-default.apps.cngateway-demo.fg1b.p1.openshiftapps.com \
-connect    localhost:8443 \
 </dev/null | openssl x509 -noout -text
```

If the route doesn't work for some reason (see the openssl troubleshooting as a way to verify), it may work after deleting it and recreating it without the port argument.  I've had that happen at least once.
## TODO

* Verify route creation ahead of time can work.  It used to work with --port 443 but in the last demo I had an issue with that and had to recreate it after the service was up without a port argument.

Issue related
https://github.com/MichaCo/DnsClient.NET/issues/4#issuecomment-819805640
