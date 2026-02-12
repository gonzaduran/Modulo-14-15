📌 README – Personalización Módulo Fichaje Odoo 17
👤 Alumno: Gonzalo Durán Parreño
🏢 Empresa: Soluciones S.A.
📖 Descripción del Proyecto

La empresa Soluciones S.A. requiere la personalización del módulo base Fichaje en Odoo 17 para adaptarlo a la identidad corporativa de cada departamento.

A partir del módulo proporcionado por la profesora:

🔗 https://github.com/mcsanchez94/fichaje.git

Se han realizado modificaciones en:

Identidad visual del módulo

Modelo de datos

Interfaz de usuario

Flujo técnico mediante Docker

✅ Requisitos Implementados
1️⃣ Identidad Visual

Se ha modificado el nombre del módulo en el tablero principal de Odoo.

🔄 Cambio realizado:

De:
Fichaje

A:
Presencia - Gonzalo

📂 Archivo modificado:
view.xml

🛠 Cambio aplicado:
'name': 'Presencia - Gonzalo',


Esto actualiza el nombre que aparece en los “cuadraditos” del dashboard principal de Odoo.

2️⃣ Modelo de Datos

Se añadió una nueva opción "Descanso" al campo tipo_accion.

📂 Archivo modificado:
models/models.py

🛠 Código modificado:

Se localizó el campo tipo Selection:

tipo_accion = fields.Selection([
    ('entrada', 'Entrada'),
    ('salida', 'Salida'),
    ('descanso', 'Descanso')
], string="Tipo de acción")


✔ Se añadió correctamente la tercera opción:

('descanso', 'Descanso')

3️⃣ Interfaz de Usuario

Se verificó que el nuevo campo sea visible en:

Vista de lista

Vista de formulario

📂 Archivo modificado:
views/view.xml


Se aseguró que el campo tipo_accion esté presente en:

<field name="tipo_accion"/>


✔ La estructura XML se mantuvo compatible con Odoo 17
✔ No se rompió ninguna etiqueta
✔ El módulo carga correctamente

🐳 Procedimiento Técnico con Docker

Para aplicar correctamente los cambios se siguió el flujo obligatorio con contenedores:

1️⃣ Levantar el contenedor
docker-compose up -d

2️⃣ Copiar el módulo modificado al contenedor
docker cp . odoo:/mnt/extra-addons/fichaje

3️⃣ Reiniciar el servicio Odoo
docker restart odoo

4️⃣ Actualizar el módulo en Odoo

Dentro de Odoo:

Ir a Apps

Buscar "Fichaje"

Activar modo desarrollador

Pulsar Actualizar módulo

✔ Esto fuerza a Odoo a leer los cambios en la base de datos.

🗄 Validación en Base de Datos (pgAdmin)

Se realizó una consulta SQL para verificar que existe un registro con:

tipo_accion = 'descanso'

📌 Consulta ejecutada:
SELECT * 
FROM fichaje_registro
WHERE tipo_accion = 'descanso';


✔ Se comprobó que el valor se almacena correctamente en la base de datos.

📸 Evidencias Entregadas

Captura del tablero de Odoo mostrando:

Nombre: Presencia - Gonzalo

Opción Descanso visible y funcional

Captura de pgAdmin mostrando la consulta SQL con registro 'descanso'

🎯 Conclusión

Se ha personalizado correctamente el módulo base de fichaje cumpliendo todos los requisitos:

✔ Cambio de identidad visual
✔ Modificación del modelo de datos
✔ Actualización de la interfaz
✔ Flujo correcto con Docker
✔ Validación mediante SQL

El sistema queda completamente operativo y adaptado a los requisitos de la empresa Soluciones S.A.
