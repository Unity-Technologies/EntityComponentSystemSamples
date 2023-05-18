using Unity.Collections;

namespace Samples.HelloNetcode
{
#if UNITY_EDITOR || NETCODE_DEBUG
    /// <summary>
    /// DO NOT SHIP GENERATED PRIVATE KEYS AS PART OF YOUR GAME!
    ///
    /// Generated SSL certificates.
    /// Follow this guide to generate new certificates: https://docs-multiplayer.unity3d.com/transport/current/secure-connection#generating-the-required-keys-and-certificates-with-openssl
    /// </summary>
    public static class SecureParameters
    {
        /// <summary>
        /// The common name used to define the server certificate
        /// </summary>
        public static FixedString512Bytes ServerCommonName = new FixedString512Bytes("hello_netcode_secure");

        /// <summary>
        /// Game client certificate
        /// </summary>
        public static FixedString4096Bytes GameClientCA = new FixedString4096Bytes(
@"-----BEGIN CERTIFICATE-----
MIIDrTCCApWgAwIBAgIUTyaYNpcFdM08QLyNUwRn1rLtIjYwDQYJKoZIhvcNAQEL
BQAwZjELMAkGA1UEBhMCREsxEzARBgNVBAgMCkNvcGVuaGFnZW4xEzARBgNVBAcM
CkNvcGVuaGFnZW4xDjAMBgNVBAoMBVVuaXR5MR0wGwYDVQQDDBRoZWxsb19uZXRj
b2RlX3NlY3VyZTAeFw0yMjA1MjMxNTI2MzNaFw0yNTA1MjIxNTI2MzNaMGYxCzAJ
BgNVBAYTAkRLMRMwEQYDVQQIDApDb3BlbmhhZ2VuMRMwEQYDVQQHDApDb3Blbmhh
Z2VuMQ4wDAYDVQQKDAVVbml0eTEdMBsGA1UEAwwUaGVsbG9fbmV0Y29kZV9zZWN1
cmUwggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC+Vo6TUtqsUL54Prfe
FoYeqzXzcNpoEQC3xD9e0DMdCaH1QbSa0cloM1xdmuf1CPXZXNEPXfKU/hbv9L9L
GNg2h91u1cnVN2eVzo+qPTwu30C29XP2fPNoHSdsnQfYc7Q+MS0+tE/V2oBbtPvM
OuMuwLpNnhxYmrx154TEcKnrHjCycKVAbaH1CTWfsjZgMv6hdHu7nw1Byu/1DVXe
Eyt+Oi6ZCnpX1NcdtyksHtsN977vzHw+SRhbvryPXwlWQq06HPHFcALzlHxCJma3
eBLTUx5KP5pPsu5BeALgwl2KjKQrlg9g5iQqSYoTcs1HQLCJeXUqVHo8dPO+Wxb6
lFNPAgMBAAGjUzBRMB0GA1UdDgQWBBSye4K17T2ntaTTKzZqnZoxYxvbEDAfBgNV
HSMEGDAWgBSye4K17T2ntaTTKzZqnZoxYxvbEDAPBgNVHRMBAf8EBTADAQH/MA0G
CSqGSIb3DQEBCwUAA4IBAQC1Z3zU+kw18GoOOKl2futM81tBCQQDh9Kvn7Qvcjbi
ZRpyJ4FBMLbDXNpn09VQdkM7ptHVog6hCSFM419HdpLyS3/K5jJc4VhSBlj1kxL8
bV9unAgRsPobFkjBORPDVdlq76wV6OQPZmDpc+eUCK9whz4Cc/a8aufqas2HvM2k
DO5xemtBEDlIuK1olwg05wtyJ7da87Zi3gRbC2+DJqJIuXsUf1/GITyQUX3b9GMO
SnncwCUciS6wbSIekPdBq3sScvKpYF6/v0gkNp8dMgfPN3PR3Ekp9aPvJLMsF8Iw
8STpmWiS2Vik4ZYgmwI9lJlN3LL9B+Yv/wSpv6YRr7HB
-----END CERTIFICATE-----");

        /// <summary>
        /// Server certificate
        /// </summary>
        public static FixedString4096Bytes GameServerCertificate = new FixedString4096Bytes(
@"-----BEGIN CERTIFICATE-----
MIIDUzCCAjsCFG+hsuFH3BLdL2h9VPaQtWntWUzcMA0GCSqGSIb3DQEBCwUAMGYx
CzAJBgNVBAYTAkRLMRMwEQYDVQQIDApDb3BlbmhhZ2VuMRMwEQYDVQQHDApDb3Bl
bmhhZ2VuMQ4wDAYDVQQKDAVVbml0eTEdMBsGA1UEAwwUaGVsbG9fbmV0Y29kZV9z
ZWN1cmUwHhcNMjIwNTIzMTUyNzMxWhcNMjMwNTIzMTUyNzMxWjBmMQswCQYDVQQG
EwJESzETMBEGA1UECAwKQ29wZW5oYWdlbjETMBEGA1UEBwwKQ29wZW5oYWdlbjEO
MAwGA1UECgwFVW5pdHkxHTAbBgNVBAMMFGhlbGxvX25ldGNvZGVfc2VjdXJlMIIB
IjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsoPwsd7IELUE0KY5TFXLkbTZ
cXQnawmfs/rAp07Za/R/AKIZYhhlKQz43BiLa3Ny62CuW5surSEs82QFbounXP/T
uZ1mRs9bLutkrHxXa+Xhsy1LatND1H4jtFHdrIKkpnxOun5xo+D0do/5tOcOZ3Mz
8ndodRJwDE9d/tS78X3YUbuQhc0AinCDWvhsqbAsyfiqQxi1rMC5dz8Mv/fz70Sh
vv5s2dzCMCQVENjAGDYsS6nCxdXnnUPj9xLZ8WDvODZROKFQAIATuX8u6VhTko0W
4QXWW3a2JIiyUp3mE4RwPdv23yYSVxFY6M5Hmeq9ujmTsaQs5a8U1bkxb4oxuwID
AQABMA0GCSqGSIb3DQEBCwUAA4IBAQALCtz69qeylJRKjWGsV9EjqxTDZWwV304X
AMYlGH9ZcR91Y74bPPlp41woOCrpuVlABg5/HTJlkMP8FGmHM+jdllvrbErzetXc
uXnUvcCvPR4wHCZ9khV84SfKKqUAjKcfV5bHVZM9G2Zx3Dqy/IVMJPVRkDVkXnlL
JlfCXqw07+961o0v74XPK7bMRFK39UK2VuFZWUHXKlYPSsE1CAGwLPdgeDS9h5vP
J1AEsAdSG2d1EEX8d6EDF0y4zZ+Jf0WWBczxr3UNt/OKHIOJo4oiNaAyJ3hCerka
Ys/Q3Kya41ODVBXq2rFoQ7+IG7iw2YGA3WISMmQmTNFLhZQd3ah/
-----END CERTIFICATE-----");

        /// <summary>
        /// Server private key
        /// </summary>
        public static FixedString4096Bytes GameServerPrivate = new FixedString4096Bytes(
@"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAsoPwsd7IELUE0KY5TFXLkbTZcXQnawmfs/rAp07Za/R/AKIZ
YhhlKQz43BiLa3Ny62CuW5surSEs82QFbounXP/TuZ1mRs9bLutkrHxXa+Xhsy1L
atND1H4jtFHdrIKkpnxOun5xo+D0do/5tOcOZ3Mz8ndodRJwDE9d/tS78X3YUbuQ
hc0AinCDWvhsqbAsyfiqQxi1rMC5dz8Mv/fz70Shvv5s2dzCMCQVENjAGDYsS6nC
xdXnnUPj9xLZ8WDvODZROKFQAIATuX8u6VhTko0W4QXWW3a2JIiyUp3mE4RwPdv2
3yYSVxFY6M5Hmeq9ujmTsaQs5a8U1bkxb4oxuwIDAQABAoIBAD8asvg9l52IT/Zt
/C1G2tpcAs2/ULvewAzAZGAAWI139XlE1BJAK2pygnpTVt2aBxK7r4cEeWCMLLax
MdZdGcGfUbn4sRHw9PvGDGWI4uJqdfl/1nwhyIWSPY7dra3w1MFhifcVAZj7yY8r
4AqZ7xaUu9VHbq6L4P4JBGIz02hPC/2iFSY9Sx4yuxIj3DWaJeV76vtstV1QiHMX
VS7u6rrzNZ37EyAKTrJqS9FppXah6Lq+KF0DaqMyKnjrN6035a3w+r3MwI9rCRx1
EG/rMIT1tkhLhgNAz+kjQe7TJOzad8Ure+kjv5QH2LxQww6nco0GW+dH7/WBkX6r
Wz3JSHkCgYEA2tvINGNPJ4izDet/sQyGwd0TNyhPae2iMyUHyfiPex3fYx4VBTiM
qDfSEJlxemOI+Aig8ZjwQXHhUl3A8/8/P51o4J/dnAfv8Td6CPGQuoeF/oT4msy9
Zpo4YP7ZBJlPW3JKaun+02ZW9B2z7BPFl+yoGFcI4suSuStR6x9yP8cCgYEA0M93
rWqcgg7yXTb+qyIJWDA0gjevnhenkmc2sbsm/f8WtzTMy28sYQlywbF6ZsxMPBEw
MBKWxtDofWMcbkdEXNRVZ66BfHmW8ml28ccptSzcYinuHKQ6wD3pKcy8GT9tTw5M
QtThIq0mxAAHJe0J++gOflo+Y0T6z0Uslokmpm0CgYBYDF/JI8LuhsJycTIYCpAU
YlqesCry1NWSee1eRg+DWotKlwmh5hRAgOSUJQdQU8cA8oe9augNjEE4H9vGzgOm
Fi/hXq+LXG4dv1HrUzQEtw0jTl+t93yjUJwp+Z1Zikww4BQsWyNX7S5CW8jMy0+N
RXqDAFDk3T2UHWeBjk8qdQKBgEO7+PtlCO3bgV0heMz0loln4bCX9bzXuYDxQDm5
FvXvqGO3mfMn1gKIORSByM3N2bDmdnYxoX1OyQvbeZ6AObnPGwEuB0zkeEWcVnwp
eesOaVLifR4HXBN+QcNQaXfbLk8luTE4sus3tcqAo9D2qnVvmjv8dB5pgt53dvZB
+SqlAoGAPFVFqFMknvDGZtmSOCDhSxADk4+e0iujwyhSF9oM7q2vQpUoq+TyNAr+
DicXGDudzNg6ZUfNRwP+vncqiXXkxLnFuyBVQjyGsGv5vn9DFaJTahKdJI1OXsXW
s2kyL+2EXBomDQ0UtSPDYvF+pOuPDiUgx7sfoU/ey7nC0waR8jI=
-----END RSA PRIVATE KEY-----");
    }
#endif
}
