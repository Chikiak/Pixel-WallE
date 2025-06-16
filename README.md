# ⭐ PixelWallE Studio 🎨

**PixelWallE Studio** es un Entorno de Desarrollo Integrado (IDE) para un lenguaje de programación de dominio específico (DSL) diseñado para crear arte pixelado. Creado para el 2º Proyecto de Programación de MATCOM (2024-2025), este proyecto implementa un compilador completo y un intérprete visual dentro de una moderna aplicación de escritorio WPF.

## ✨ Características Principales

### Entorno de Desarrollo (IDE)
-   **Editor de Código Inteligente**: Basado en AvalonEdit, con resaltado de sintaxis para el lenguaje PixelWallE y numeración de líneas.
-   **Canvas de Renderizado en Tiempo Real**: Visualiza el arte generado por tu código al instante.
-   **Control Total de Ejecución**:
    -   **Run/Stop**: Compila y ejecuta el código, con la capacidad de detener ejecuciones largas.
    -   **Modos de Ejecución**: Elige entre `Instant`, `Step-by-Step` (por comando) y `Pixel-by-Pixel` para depurar o disfrutar del proceso de dibujo.
    -   **Control de Velocidad**: Ajusta el retardo entre pasos con un slider intuitivo.
-   **Gestión de Archivos**: Crea, abre, guarda y "guarda como" archivos de código (`.pw`).
-   **Herramientas del Canvas**:
    -   Define dimensiones personalizadas para el lienzo.
    -   Carga una imagen como fondo para trazar o editar.
    -   Exporta tu creación como imagen (`PNG`, `JPG`, `BMP`).
-   **Consola de Salida**: Un panel claro que muestra mensajes de estado, errores de compilación y de ejecución, formateados por color para una fácil identificación.

### Motor del Lenguaje
-   **Pipeline de Compilación Robusto**: El código pasa por un pipeline clásico: **Lexer → Parser → Analizador Semántico**, asegurando que solo el código válido se ejecute.
-   **Reporte de Errores Detallado**: Identifica múltiples errores en una sola compilación, indicando la línea y columna exactas:
    -   **Errores Léxicos**: Caracteres inválidos.
    -   **Errores Sintácticos**: Comandos malformados o incompletos.
    -   **Errores Semánticos**: Tipos incompatibles, variables no declaradas, etiquetas faltantes.
    -   **Errores de Ejecución**: División por cero, coordenadas fuera de los límites, etc.
-   **Intérprete Asíncrono**: Procesa el AST y dibuja sobre un bitmap de SkiaSharp, permitiendo una UI siempre responsiva.

---

## 📖 Guía del Lenguaje PixelWallE

### Comandos Principales

| Comando | Sintaxis | Descripción |
| :--- | :--- | :--- |
| **`Spawn`** | `Spawn(int x, int y)` | Establece la posición inicial. **Debe ser el primer comando y solo puede usarse una vez.** |
| **`Color`** | `Color(string color)` | Cambia el color del pincel. Acepta nombres (`"red"`) y hexadecimales (`"#FF0000"`). |
| **`Size`** | `Size(int k)` | Modifica el grosor del pincel (en píxeles). |
| **`DrawLine`** | `DrawLine(int dX, int dY, int dist)` | Dibuja una línea en la dirección (`dX`, `dY`) por `dist` píxeles. |
| **`DrawCircle`**| `DrawCircle(int dX, int dY, int r)` | Dibuja un círculo de radio `r` desplazado del punto actual. |
| **`DrawRectangle`**| `DrawRectangle(int dX, int dY, int dist, int w, int h)`| Dibuja el contorno de un rectángulo. |
| **`Fill`** | `Fill()` | Rellena un área con el color del pincel actual (algoritmo flood fill). |

### Expresiones, Variables y Control de Flujo

El lenguaje soporta variables, operaciones matemáticas, lógica booleana y saltos condicionales.

-   **Asignación de Variables**:
    ```pw
    mi_variable <- 10 * (5 + 2)
    es_valido <- 1
    ```
-   **Operadores Aritméticos**: `+`, `-`, `*`, `/`, `%` (módulo), `**` (potencia).
-   **Operadores Lógicos y de Comparación**: `and`, `or`, `==`, `!=`, `>`, `>=`, `<`, `<=`.
-   **Control de Flujo con `GoTo`**:
    ```pw
    # Define una etiqueta
    mi-loop

    # ... código ...

    # Salta a la etiqueta si la condición es verdadera (distinto de 0)
    GoTo [mi-loop] (i < 10)
    ```

### Código de Ejemplo

```pw
Spawn(0,0)
Color("blue")
Size(1)


i <- 20
b <- true

GoTo[comenzar_circulo](true)



circulo_azul
Color("#8000adad")
DrawCircle(1,1,3)
i <- i - 1
GoTo[circulo_terminado](true)



circulo_rojo
Color("#80FF0000")
DrawCircle(1,1,4)
i <- i - 1
GoTo[circulo_terminado](true)



circulo_terminado
Color("transparent")
#DrawLine(1,1,2)


comenzar_circulo
GoTo[circulo_azul](i >= 0 and i%2 == 0)
GoTo[circulo_rojo](i >= 0 and i%2 == 1)
```

---

## 🛠️ Arquitectura y Decisiones Técnicas

Este proyecto se construyó aplicando patrones de diseño y prácticas de software modernas para garantizar un código robusto, extensible y mantenible.

-   **Patrón MVVM**: Utilizado en `PixelWallE.WPF` para separar la interfaz de usuario de la lógica de la aplicación, usando `CommunityToolkit.Mvvm` para una implementación eficiente.
-   **Patrón Visitor**: Es la piedra angular del compilador. Permite procesar el AST para el análisis semántico (`CheckSemantic`), la ejecución (`Interpreter`) y la depuración (`ASTPrinter`) sin modificar las clases del AST.
-   **Inyección de Dependencias (DI)**: La `MainViewModel` recibe sus servicios (`ICompilerService`, `IExecutionService`) vía constructor, lo que desacopla los componentes y facilita las pruebas.
-   **Patrón Strategy**: Los `ExecutionMode` actúan como estrategias que modifican el comportamiento del `Interpreter`, cambiando cómo y cuándo se reporta el progreso visual.
-   **Manejo de Errores con `Result<T>`**: En lugar de excepciones, el pipeline de compilación usa un tipo `Result<T>` para encapsular un resultado exitoso o una lista de errores. Esto permite un manejo de errores más limpio y la capacidad de reportar múltiples fallos a la vez.

### Tecnologías Clave

-   **.NET 9**
-   **WPF** para la interfaz de escritorio.
-   **SkiaSharp** para el renderizado de gráficos 2D de alto rendimiento.
-   **AvalonEdit** para el componente de editor de texto.
-   **CommunityToolkit.Mvvm** para una implementación moderna de MVVM.

---

## 🚀 Cómo Empezar
**Opción A:**
1. Clona este repositorio.
2. Abre la carpeta Ejecutable y dento esta el PixelWallE.WPF.exe y listo


**Opción B:**
1.  Clona este repositorio.
2.  Abre la solución `PixelWallE.sln` en Visual Studio 2022 o superior / Rider.
3.  Asegúrate de tener instalado el workload de ".NET desktop development".
4.  Establece `PixelWallE.WPF` como el proyecto de inicio.
5.  Presiona `F5` para compilar y ejecutar.

---

## 👨‍💻 Autor

-   **Adrián Estévez Álvarez**

*Proyecto para el curso de Programación, Facultad de Matemática y Computación (MATCOM), Universidad de La Habana.*