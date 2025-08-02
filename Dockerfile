FROM owasp/modsecurity-crs:nginx

USER root

ENV DISABLE_DEFAULT_TEMPLATE=true
ENV TEMPLATE_NGINX_CONF=""

RUN rm -f /etc/nginx/templates/nginx.conf.template

# Копируем свои конфиги
COPY ./unicode.mapping /etc/modsecurity/unicode.mapping
COPY ./modsecurity.conf /etc/modsecurity/modsecurity.conf
COPY ./nginx.conf /etc/nginx/nginx.conf

# Копируем CRS в образ
COPY ./coreruleset /etc/modsecurity/owasp-crs

## Обязательно включаем crs-setup.conf и правила
#RUN cp /etc/modsecurity/owasp-crs/crs-setup.conf.example /etc/modsecurity/owasp-crs/crs-setup.conf