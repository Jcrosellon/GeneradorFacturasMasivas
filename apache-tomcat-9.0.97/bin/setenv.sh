# Opciones de JAVA
export JAVA_OPTS="-Djava.security.egd=file:/dev/urandom"

# Opciones de CATALINA
export CATALINA_OPTS="$CATALINA_OPTS -Drs.configdir=/Users/mariapaz/GeneradorFacturasMasivas/apache-tomcat-9.0.97/webapps/reportserver/WEB-INF"

# Opciones de memoria y ajustes para Java 17 (elimina MaxPermSize)
export CATALINA_OPTS="$CATALINA_OPTS -Xmx4096M -Dfile.encoding=UTF8 \
--add-opens=java.base/java.net=ALL-UNNAMED \
--add-opens=java.base/jdk.internal.ref=ALL-UNNAMED \
--add-opens=java.base/jdk.internal.reflect=ALL-UNNAMED \
--add-opens=java.base/java.lang.invoke=ALL-UNNAMED \
--add-opens=java.base/java.util=ALL-UNNAMED \
--add-opens=java.base/java.lang.ref=ALL-UNNAMED \
--add-opens=java.base/java.lang.reflect=ALL-UNNAMED \
--add-opens=java.base/sun.reflect.generics.repository=ALL-UNNAMED"
