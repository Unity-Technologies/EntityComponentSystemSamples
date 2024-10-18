//#define ENABLE_NETCODE_SAMPLE_SECURE
using Unity.Collections;

namespace Samples.HelloNetcode
{
#if ENABLE_NETCODE_SAMPLE_SECURE
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
MIICujCCAaICCQDGfCe0Uf7HbTANBgkqhkiG9w0BAQsFADAfMR0wGwYDVQQDDBRo
ZWxsb19uZXRjb2RlX3NlY3VyZTAeFw0yNDA0MTcxNDM2MDZaFw0zNDA0MTUxNDM2
MDZaMB8xHTAbBgNVBAMMFGhlbGxvX25ldGNvZGVfc2VjdXJlMIIBIjANBgkqhkiG
9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnPD5oaBDFAZP8sw/cCMV7EedDMxU4I+KE6Pi
W3y3ksNIPEHT1pcPcNFe09jUMnDT1DCfQHBJB3+5Oh6D/24STUSVZQaUsECdQJD8
i+baqvFVPGZKIoQ5zpAOmyJU9dqmMl7kWhxO57dd8ZjBJy4KKiEs+yUMK8gx8kFC
oxEv4p6SHISaVNjO2OAgHLsjpWkdFmzvIdc7awOMamFyFUZfV5ZahfaUkQt3eY2a
G4Tgym/K8gWSlKDw8YnX3Xzfnx8BVy96lTbsMAAFueXznAgTXaDxGrJWWOn1+TRT
U4aF7kd6ssVApzu1N1VjBBYx/pxre9kTk2iCqioHD3fGCZ2EuwIDAQABMA0GCSqG
SIb3DQEBCwUAA4IBAQAFFBJuwRQYsi2dYC+bXkc+JmqJM0ZilA8m70tQtYd13eX2
yqM8RpzL3L+YdVp3qDlxrHiArkPtkFVdDl8pDF9neUf6bCyZW82M88vDnYmpSLT3
aqXWAc/CxQKMO4qqUIdko3+ki1b6BgYvldZJAWYBbtMuN7oNJHQgdrqCCvBl1nn3
i9eCU9CaNpzfZAmBU6CfK7QP/J1Krx6PvSCIJeP82qgmNX6xX0mWO964Vs+C4Gvp
+bC/5wyBuIWwFWsgLGGs0LEj8Q4HH3/gPohptAFQBDmWvGHRk1UA6miYZCk5RYoX
k3lUTmFuSP28wc9J8HGp7K7rAY7fZ+X2bNUyys2x
-----END CERTIFICATE-----");

        /// <summary>
        /// Server certificate
        /// </summary>
        public static FixedString4096Bytes GameServerCertificate = new FixedString4096Bytes(
@"-----BEGIN CERTIFICATE-----
MIICujCCAaICCQCucODkG5ZoXjANBgkqhkiG9w0BAQsFADAfMR0wGwYDVQQDDBRo
ZWxsb19uZXRjb2RlX3NlY3VyZTAeFw0yNDA0MTcxNDQ3MzBaFw0zNDA0MTUxNDQ3
MzBaMB8xHTAbBgNVBAMMFGhlbGxvX25ldGNvZGVfc2VjdXJlMIIBIjANBgkqhkiG
9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvcB8ESNmhZKktiMlo1tTjhl81UcJdvq7+Wfu
b7ORfFoT7XcJaLZ+s88jdvTAu5KYb9yiCcjPz6uaqErabYtIna1v3g4qV/2D8J94
qDMFWbb87Qa5Igy0yK72CW/sDK+MtwxWRJ//+k0mEOpjlSrFX7Yuz2kytiRZiOIq
+IOdIA8sogXaQvPm4Oz9rUOqSws8JYGnm9ZPfB0JQLCi4wC4vQ4ZB3SB5AluS0Sr
s/lo/rPNBX9QAyQzqD/6QiP0TkW+Eh1cQxXPMijE3683qKFRJubkLDGn1RCoetEe
+0t8LdHam+TUZG5CCHR6D/JxKPJpRDu/IEsBOcthxB+Rv68e0QIDAQABMA0GCSqG
SIb3DQEBCwUAA4IBAQCcuUddfnU8sseUpI/46xp1CE8FQPGjoANWsW6FwoJ8Yfcc
I9WrzREUbL6DywnugphKu116ppHAYnunKetzwWMzo3NSaKOCUDOW4PBdleWA/+oU
neTW+aOotZBHwMiAOwQ7ALFAfjJepEVl10UAu2XSAkIeEZw4Nl7rgKGm2gOK3SZF
E4AsInFowwGHFNH8DrbD0zifA9ueg9sL6MB3ttKjmSHX1D/6WMxWcgf0axSp+yDc
gARkfx77Hh2Y6FLG5eo8Io/XJz0gg7T9ohbCp+4lZWEZIep6M4kPK+buFYqkSWNh
JuNQ8OLhdJVoOVYYyfCdZYyUNXGfLDXF25KESoJa
-----END CERTIFICATE-----");

        /// <summary>
        /// Server private key
        /// </summary>
        public static FixedString4096Bytes GameServerPrivate = new FixedString4096Bytes(
@"-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEAvcB8ESNmhZKktiMlo1tTjhl81UcJdvq7+Wfub7ORfFoT7XcJ
aLZ+s88jdvTAu5KYb9yiCcjPz6uaqErabYtIna1v3g4qV/2D8J94qDMFWbb87Qa5
Igy0yK72CW/sDK+MtwxWRJ//+k0mEOpjlSrFX7Yuz2kytiRZiOIq+IOdIA8sogXa
QvPm4Oz9rUOqSws8JYGnm9ZPfB0JQLCi4wC4vQ4ZB3SB5AluS0Srs/lo/rPNBX9Q
AyQzqD/6QiP0TkW+Eh1cQxXPMijE3683qKFRJubkLDGn1RCoetEe+0t8LdHam+TU
ZG5CCHR6D/JxKPJpRDu/IEsBOcthxB+Rv68e0QIDAQABAoIBABHMMxbcbipLJd3b
kBUxZLXoWBgdEJszS1xKTkf13MiAHmxghOZob5vn6timfklZp6ieVih6yFsfKmNs
me46aTY45Uw7oecc5To1ivijyHWwvypwPf8el/pWxsb903MhKB6nLpRDOZw9jjt5
8Js2JssiaGOV52bEJA29wPAMUDmIaWEc5pmofv+lm02YRBCGxR+cFMhXjqULQk9Y
1oLzzZq844GsxdVovt3qc9vwyFOV2CvL+VSPsJ7HK3pYadUSwgdUfH+xeZFJy23z
dgtNvEU8aYyM3SCbFMPM9i1lY1CDK6PK5Pe/p/XrwSqVTmYJZDUEzvSbyRacHC49
MHar3tECgYEA7KYdf2EVvWxPcrZhmvUwX+gglOBtgjfQPKO0hyjJ5kHw7elGQdcb
KFiMpUe0SjIglGdSHvhFj+UvQ4fL4an55wfLkisSegfdydUIR6V9SRG6GmDT16j+
OeOEIHPIH6ruhkiyklrrsQHFD+qytgKDBKKj+ZJooXIfjSL93eUu6nUCgYEAzUSm
8Mkq44Z/hJWIKN+Laeee8XsNONsa+buMCz6C7bHxDgZ45qDVm2ulgHjSClXgPEGM
jm7R7hTIvP67WauPTYU87obgbBSfKNz4ezRAPm6vpmWGI+dDedcn7hBsN9M0He+4
/msaYSaDzqpqC2NqArQf9M4yblb3zV5Qxnmov20CgYBW5ZSVTpAOE3SE+eWTYg9W
WEWGhXaQx2/mpHJI4zhoHbSbl/odeSBWy1Ux58eTKx79f4cPKjlY4l5dnMLH5YOH
Szx8Oua4+qR9VYWJ0YHUz/aXcxC28y4PEbVVuU42Gq0lkBJKXaqIP88dzh+7Z+a2
UAaIQTO8fMyLJds0nNCCdQKBgA8wz3HuUUA5SeKT9lmgAX865uZUBux4OozUtk52
t9XDX2V8USIwMN6pnrvdNR4SsN+EslQwG1UVMK3b5B2Etrwz6gh07tLQy96IS9NC
UKbOJi2YQc8SZEn2BDx39qpC9Q5qGTSq1G7wHL0Em4hwOP4uOlcxk0XbJceK/UtS
4YwZAoGAedAZ2aHKRRPX969fnrrJ5uravgQ1PFyegJLPqx7eIjFmsPw8B5t9N9ql
AgtYiWiY8QnhXnlYyH8jki3gipYQ+hb2sKzKnupvEAhKmiyGq9mgBrooX5aE2u4N
25F2jhuygG0m/O7dsp1/IIy8uORgyVhQCagvDP+J8fh330fLoLI=
-----END RSA PRIVATE KEY-----");
    }
#endif
}
