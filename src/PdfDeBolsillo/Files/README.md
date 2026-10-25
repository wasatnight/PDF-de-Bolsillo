# Capa de archivos
DocumentSession crea copias temporales y conserva un registro de rutas originales durante la sesión.
PathSafety resuelve rutas de Windows (incluidos enlaces/junctions) y bloquea originales y destinos duplicados.
PdfExportService escribe temporales junto al destino, exige aprobación para reemplazos, vuelve a comprobar los destinos y revierte la publicación de lotes cuando es posible.
No existe transacción global entre varios archivos frente a apagados o fallos de reversión. En esos casos se informa y se conservan respaldos.
