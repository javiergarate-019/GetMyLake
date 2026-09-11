# Proyecto: buscar lagos del mundo con una forma parecida a Uruguay

## Objetivo

Desarrollar un programa en **C#** que busque, entre los lagos del mundo,
cuáles tienen una silueta geométrica más parecida al contorno de
**Uruguay**.

El resultado final debería ser un ranking, por ejemplo con los **20
lagos más parecidos a Uruguay**, incluyendo:

-   Nombre del lago.
-   País o países.
-   Identificador del lago en el dataset.
-   Superficie real.
-   Porcentaje o score de similitud.
-   Ángulo de rotación que produce la mejor coincidencia.
-   Idealmente, una imagen comparativa con el contorno del lago y
    Uruguay superpuestos.

La comparación debe ignorar la ubicación geográfica y el tamaño real:
interesa únicamente la **forma**.

------------------------------------------------------------------------

## Tecnología

Implementar preferentemente como una aplicación de consola en:

-   **C#**
-   .NET
-   **NetTopologySuite** para geometrías y operaciones espaciales.

Evitar dependencias de aplicaciones GIS externas como QGIS o ArcGIS.

El programa debe poder ejecutarse desde PowerShell.

------------------------------------------------------------------------

## Fuentes de datos

### Lagos: HydroLAKES

Usar **HydroLAKES**, de HydroSHEDS:

https://www.hydrosheds.org/products/hydrolakes

HydroLAKES contiene aproximadamente **1,4 millones de lagos y embalses**
de todo el mundo, con una superficie mínima aproximada de 10 hectáreas.

Preferentemente descargar la versión global en **Shapefile**.

Debe conservarse, cuando esté disponible, la metadata necesaria para
identificar:

-   nombre;
-   país;
-   superficie;
-   identificador HydroLAKES;
-   información que permita distinguir lago natural de embalse, si el
    dataset lo permite.

Inicialmente se pueden considerar tanto lagos naturales como embalses.
Posteriormente sería útil generar rankings separados.

### Contorno de Uruguay

Usar **Natural Earth -- Admin 0 Countries, escala 1:10m**:

https://www.naturalearthdata.com/downloads/10m-cultural-vectors/10m-admin-0-countries/

Extraer de ese dataset el polígono correspondiente a Uruguay.

La escala 1:10m debería proporcionar suficiente detalle para esta
comparación sin introducir una cantidad innecesaria de vértices.

------------------------------------------------------------------------

## Concepto de comparación

Queremos comparar exclusivamente la silueta.

Por lo tanto, antes de comparar dos geometrías hay que eliminar las
diferencias debidas a:

1.  posición;
2.  tamaño;
3.  rotación.

Inicialmente **NO permitir reflexión especular**.

Más adelante puede hacerse un segundo ranking permitiendo reflexión para
responder también a la pregunta:

> ¿Qué lagos se parecen a Uruguay incluso si aceptamos una imagen en
> espejo?

------------------------------------------------------------------------

## Normalización de las geometrías

Para Uruguay y cada lago candidato:

1.  Validar/reparar la geometría si fuera necesario.
2.  Trabajar con el polígono principal cuando existan geometrías
    multipoligonales, tomando una decisión razonable sobre islas.
3.  Centrar la geometría alrededor de `(0,0)`.
4.  Normalizar el tamaño.
5.  Simplificar razonablemente el contorno para evitar que diferencias
    minúsculas de resolución dominen la comparación.

### Normalización de escala

Una opción recomendable es escalar cada geometría para que tenga:

``` text
Área = 1
```

De esta manera se compara la forma independientemente del tamaño real
del lago.

También puede investigarse si conviene normalizar por bounding box, pero
la normalización por área parece conceptualmente más apropiada.

------------------------------------------------------------------------

## Estrategia de rendimiento

No conviene realizar una comparación geométrica completa de Uruguay
contra los aproximadamente 1,4 millones de lagos para cientos de
rotaciones.

Implementar una búsqueda en dos etapas.

### Etapa 1: filtro barato

Calcular características geométricas simples tanto para Uruguay como
para cada lago.

Posibles características:

-   relación ancho/alto del bounding box;
-   compacidad;
-   perímetro² / área;
-   elongación;
-   convexidad;
-   relación área / área del convex hull;
-   otras métricas simples que sean invariantes ante escala.

Descartar rápidamente formas obviamente diferentes.

La idea es reducir:

``` text
~1.400.000 lagos
        ↓
unos pocos miles de candidatos
```

Los umbrales no deben ser excesivamente estrictos al principio, para no
eliminar accidentalmente un lago visualmente parecido.

Guardar suficientes estadísticas para poder revisar posteriormente el
efecto de los filtros.

------------------------------------------------------------------------

## Etapa 2: comparación geométrica fina

Para los candidatos restantes realizar alineamiento y comparación del
polígono completo.

### Rotación

Permitir rotación libre.

Una implementación inicial sencilla puede probar:

``` text
0°
1°
2°
...
359°
```

y conservar el ángulo con mejor resultado.

Después puede optimizarse:

1.  búsqueda gruesa, por ejemplo cada 5°;
2.  localizar el mejor intervalo;
3.  refinar alrededor del mejor ángulo;
4.  opcionalmente realizar una optimización continua.

No asumir que el norte del lago debe corresponder al norte de Uruguay.

------------------------------------------------------------------------

## Métrica principal propuesta: Intersection over Union

Después de:

-   centrar;
-   normalizar;
-   rotar;

superponer ambas geometrías y calcular:

``` text
IoU = Área(Intersección) / Área(Unión)
```

Interpretación:

``` text
1.00 = coincidencia perfecta
0.90 = extremadamente parecido
0.80 = bastante parecido
...
0.00 = ninguna coincidencia
```

El score puede mostrarse como porcentaje:

``` text
Similitud = IoU * 100
```

Para cada lago conservar:

-   mejor IoU;
-   mejor ángulo de rotación.

------------------------------------------------------------------------

## Métricas alternativas

Conviene diseñar el código para poder experimentar con otras métricas
además de IoU.

Por ejemplo:

-   Hausdorff Distance;
-   distancia entre contornos;
-   Chamfer Distance;
-   momentos de Hu;
-   Procrustes/alineamiento de puntos;
-   Fourier descriptors.

IoU debe ser la primera implementación porque tiene una interpretación
visual sencilla.

Puede resultar útil combinar varias métricas si IoU produce resultados
que matemáticamente puntúan bien pero visualmente no parecen Uruguay.

------------------------------------------------------------------------

## Problemas geométricos a considerar

### Islas

Uruguay puede contener pequeñas islas en el dataset y muchos lagos
también pueden tener islas.

Hay que decidir si:

-   se ignoran;
-   se consideran agujeros;
-   se conserva únicamente el contorno exterior.

Para una primera versión probablemente sea mejor comparar principalmente
el **contorno exterior**.

### Multipolígonos

Si una entidad HydroLAKES contiene varias partes, determinar si
corresponde:

-   conservar todas;
-   usar solamente el polígono de mayor área.

Documentar la decisión.

### Geometrías inválidas

Algunas operaciones de intersección/unión pueden fallar con polígonos
inválidos.

Implementar validación y reparación utilizando las herramientas
disponibles en NetTopologySuite.

### Proyección

Los datasets estarán probablemente expresados en coordenadas
geográficas.

No calcular métricas de forma directamente sobre latitud/longitud si la
distorsión de la proyección puede afectar la silueta.

Antes de medir/comparar, transformar cada geometría a un sistema
cartesiano apropiado o utilizar una transformación local que preserve
razonablemente la forma.

Este punto es importante especialmente para lagos situados en latitudes
altas.

------------------------------------------------------------------------

## Resultado esperado

Generar un ranking, por ejemplo:

    Puesto Lago     País          Área km²   Similitud   Rotación
  -------- -------- ----------- ---------- ----------- ----------
         1 Lake X   Canadá           123.4      91.3 %        73°
         2 Lake Y   Finlandia         88.1      89.7 %       211°
         3 Lake Z   Rusia             55.7      87.9 %        18°

Guardar el resultado completo también como:

``` text
results.csv
```

El CSV debería contener al menos:

``` text
Rank
LakeId
LakeName
Country
AreaKm2
Similarity
BestRotation
NaturalOrReservoir
```

------------------------------------------------------------------------

## Visualización de finalistas

Para los mejores resultados generar automáticamente imágenes.

Cada imagen debería mostrar:

-   contorno normalizado de Uruguay;
-   contorno normalizado del lago;
-   ambos centrados y con la mejor rotación;
-   score IoU;
-   nombre del lago;
-   país;
-   ángulo.

Sería útil generar, además, una imagen lado a lado:

``` text
Uruguay | Lago | Superposición
```

para evaluar visualmente la calidad de la métrica.

No es necesario desarrollar una GUI: pueden generarse PNG
automáticamente.

------------------------------------------------------------------------

## Rankings deseados

Como mínimo:

### Ranking general

Todos los cuerpos de agua incluidos en HydroLAKES.

### Lagos naturales

Excluir embalses cuando la metadata permita hacerlo.

### Embalses

Ranking separado de embalses.

### Con reflexión

Opcionalmente repetir el algoritmo permitiendo:

``` text
rotación + reflexión especular
```

pero mantener separado este resultado del ranking principal.

------------------------------------------------------------------------

## Arquitectura sugerida

Separar responsabilidades en clases, por ejemplo:

``` text
HydroLakesReader
CountryShapeReader
GeometryNormalizer
ShapeFeatures
CandidateFilter
ShapeMatcher
RotationOptimizer
SimilarityMetrics
ResultWriter
ComparisonRenderer
```

Ejemplo conceptual:

``` text
HydroLAKES
    ↓
HydroLakesReader
    ↓
ShapeFeatures
    ↓
CandidateFilter
    ↓
GeometryNormalizer
    ↓
RotationOptimizer
    ↓
ShapeMatcher / IoU
    ↓
Ranking
    ↓
CSV + PNG
```

------------------------------------------------------------------------

## Rendimiento

El dataset es grande, por lo que evitar cargar innecesariamente todas
las geometrías completas simultáneamente si no hace falta.

Posibles optimizaciones:

-   lectura secuencial del Shapefile;
-   cálculo de features baratos en una primera pasada;
-   conservar solamente candidatos;
-   paralelizar comparaciones finas;
-   cachear la geometría normalizada de Uruguay;
-   reducir vértices mediante simplificación;
-   búsqueda gruesa/fina de rotación.

Medir tiempos de cada etapa.

Mostrar progreso en consola, por ejemplo:

``` text
Reading HydroLAKES...
Processed: 250,000 / 1,400,000
Candidates: 3,821

Fine matching...
500 / 3,821

Best so far:
Lake XXXXX
IoU: 0.8732
Rotation: 127.4°
```

------------------------------------------------------------------------

## Requisitos de robustez

El programa no debe detenerse por una única geometría problemática.

Registrar:

-   ID del lago;
-   tipo de error;
-   etapa donde ocurrió.

Continuar procesando los demás.

Generar opcionalmente:

``` text
errors.log
```

------------------------------------------------------------------------

## Primera versión

Priorizar una versión funcional antes de optimizaciones sofisticadas.

Orden sugerido de implementación:

1.  Crear proyecto C#.
2.  Incorporar NetTopologySuite.
3.  Leer Natural Earth.
4.  Extraer Uruguay.
5.  Leer HydroLAKES.
6.  Implementar normalización.
7.  Calcular características geométricas.
8.  Implementar filtro inicial.
9.  Implementar rotación.
10. Implementar IoU.
11. Procesar candidatos.
12. Ordenar resultados.
13. Generar CSV.
14. Generar imágenes de los mejores resultados.
15. Evaluar visualmente.
16. Ajustar filtros/métrica si fuera necesario.

------------------------------------------------------------------------

## Criterio principal

El objetivo no es encontrar una equivalencia cartográfica exacta, sino
responder de manera cuantitativa y visual a:

> **¿Qué lago del mundo tiene una forma más parecida a Uruguay?**

Por eso el ranking matemático debe finalmente verificarse mediante las
imágenes superpuestas.

Si una métrica produce resultados que tienen un score alto pero que un
observador humano considera poco parecidos, ajustar la métrica o
combinarla con otras medidas de forma.

El código debe quedar preparado para experimentar con distintos
criterios sin tener que reescribir todo el procesamiento.
