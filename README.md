# ⭐ PixelWallE Studio 🎨

**PixelWallE Studio** es un entorno de desarrollo integrado (IDE) creado como parte del 2do Proyecto de Programación de MATCOM (2024-2025). La aplicación permite a los usuarios crear arte pixelado (pixel-art) escribiendo comandos en un lenguaje de programación propio, diseñado específicamente para esta tarea. Wall-E, nuestro robot, se ha cansado de las figuras geométricas y ahora se dedica al pixel-art, y esta herramienta es su pincel.

Este proyecto implementa un compilador completo (Lexer, Parser, Analizador Semántico) y un intérprete visual para un lenguaje de dominio específico (DSL), todo ello envuelto en una moderna interfaz de escritorio construida con WPF.

## ✨ Características Principales

### Interfaz Gráfica (IDE)
*   **Editor de Código Inteligente**: Un editor de texto basado en AvalonEdit con numeración de líneas y resaltado de sintaxis (potencialmente extensible).
*   **Canvas de Dibujo Dinámico**: Un lienzo visual que renderiza en tiempo real el resultado del código ejecutado.
*   **Control de Ejecución Completo**:
    *   **Run**: Compila y ejecuta el código.
    *   **Stop**: Detiene una ejecución en curso.
    *   **Modos de Ejecución**: Permite elegir entre `Instant`, `StepByStep` y `PixelByPixel` para visualizar el proceso de dibujo.
    *   **Control de Velocidad**: Un slider para ajustar el retardo entre pasos en los modos de ejecución no instantáneos.
*   **Gestión de Archivos**: Soporte para crear, abrir, guardar y "guardar como" archivos de código con la extensión `.pw`.
*   **Gestión del Canvas**:
    *   Dimensiones del canvas personalizables.
    *   Posibilidad de cargar una imagen de fondo.
    *   Opción para limpiar el canvas (`Reset Canvas`).
    *   Guardar la imagen resultante en formato PNG, JPG o BMP.
*   **Consola de Salida**: Muestra mensajes de estado, errores de compilación y errores de ejecución de forma clara y formateada por colores.

### Motor del Lenguaje
*   **Compilador Robusto**: El código pasa por un pipeline de compilación: **Lexer → Parser → Analizador Semántico**.
*   **Reporte de Errores Detallado**: El sistema es capaz de identificar y reportar múltiples errores con su ubicación (línea y columna):
    *   **Errores Léxicos**: Caracteres no válidos.
    *   **Errores Sintácticos**: Comandos malformados.
    *   **Errores Semánticos**: Uso incorrecto de tipos, variables no declaradas, etiquetas faltantes.
    *   **Errores de Ejecución (Runtime)**: División por cero, posiciones fuera del canvas, etc.
*   **Intérprete Visual**: El intérprete procesa el árbol de sintaxis abstracto (AST) y dibuja sobre un bitmap de SkiaSharp, gestionando la posición, el color y el tamaño del pincel de Wall-E.

---

## 📖 El Lenguaje PixelWallE

El proyecto define un lenguaje de programación sencillo pero potente para el dibujo de pixel-art.

### Instrucciones (Comandos)

| Comando | Sintaxis | Descripción |
| :--- | :--- | :--- |
| **Spawn** | `Spawn(int x, int y)` | Inicializa la posición de Wall-E en el canvas. **Debe ser el primer comando y solo puede usarse una vez.** |
| **Color** | `Color(string color)` | Cambia el color del pincel. Acepta nombres ("Red", "Blue") y códigos hexadecimales ("#FF0000"). |
| **Size** | `Size(int k)` | Modifica el grosor del pincel. Si `k` es par, se usa `k-1`. |
| **DrawLine** | `DrawLine(int dirX, int dirY, int dist)` | Dibuja una línea de `dist` píxeles en la dirección (`dirX`, `dirY`). |
| **DrawCircle** | `DrawCircle(int dirX, int dirY, int r)` | Dibuja la circunferencia de un círculo de radio `r`. |
| **DrawRectangle** | `DrawRectangle(int dX, int dY, int dist, int w, int h)` | Dibuja un rectángulo de ancho `w` y alto `h`. |
| **Fill** | `Fill()` | Rellena un área del mismo color que el píxel actual con el color del pincel actual. |

### Expresiones y Variables
El lenguaje soporta variables, expresiones aritméticas y booleanas.

*   **Asignación de variables**: `mi_variable <- 10 * (5 + 2)`
*   **Operaciones Aritméticas**: Suma (`+`), Resta (`-`), Multiplicación (`*`), División (`/`), Potencia (`**`) y Módulo (`%`).
*   **Operaciones Lógicas y de Comparación**: `and`, `or`, `==`, `!=`, `>`, `>=`, `<`, `<=`.

### Funciones Nativas

| Función | Descripción |
| :--- | :--- |
| **GetActualX()** | Retorna la coordenada X actual de Wall-E. |
| **GetActualY()** | Retorna la coordenada Y actual de Wall-E. |
| **GetCanvasSize()** | Retorna el tamaño del canvas (ancho/alto). |
| **GetColorCount(...)** | Cuenta píxeles de un color en un área rectangular. |
| **IsBrushColor(string c)** | Retorna `1` si el pincel es del color `c`, `0` si no. |
| **IsBrushSize(int s)** | Retorna `1` si el pincel tiene tamaño `s`, `0` si no. |
| **IsCanvasColor(...)**| Retorna `1` si un píxel específico es de un color dado. |

### Control de Flujo
Se implementan saltos condicionales mediante etiquetas y el comando `GoTo`.

*   **Etiquetas**: Se define una etiqueta simplemente escribiendo su nombre en una línea.
    ```pw
    mi-etiqueta
    ```
*   **Saltos Condicionales**: Se salta a una etiqueta si la condición se evalúa como verdadera.
    ```pw
    GoTo [mi-etiqueta] (i < 10)
    ```

### Código de Ejemplo

```pw
# PixelWallE Sample Code
# Welcome to PixelWallE Studio!

Spawn(250, 250)

# Draw a blue circle
Color("blue")
Size(3)
DrawCircle(0, 0, 100)

# Fill with yellow
Color("#FFFF00")
Fill()

# This part will cause a semantic error
Color("invalid-color")
```

---

## 🛠️ Arquitectura y Detalles Técnicos

### Pipeline de Compilación

El proceso desde el código fuente hasta la imagen final sigue un pipeline clásico de un compilador:

1.  **Lexer (`Lexer.cs`)**: El código fuente en formato `string` es procesado y dividido en una secuencia de `Tokens`.
2.  **Parser (`Parser.cs`)**: La secuencia de tokens se organiza en un **Árbol de Sintaxis Abstracto (AST)**, que representa la estructura jerárquica del programa.
3.  **Análisis Semántico (`CheckSemantic.cs`)**: Se recorre el AST para validar la lógica del programa, verificando tipos, declaración de variables, etc.
4.  **Intérprete (`Interpreter.cs`)**: Si no hay errores, el intérprete recorre el AST y ejecuta las acciones. Utiliza **SkiaSharp** para el dibujo.

### Estructura del Proyecto

El proyecto está organizado en una solución de .NET con una arquitectura clara y desacoplada:

*   📂 `PixelWallE.Core`: El corazón del proyecto. Contiene toda la lógica del compilador y el intérprete.
*   📂 `PixelWallE.WPF`: La interfaz de usuario de escritorio (IDE) que implementa el patrón **MVVM**.
*   📂 `PixelWallE.Console`: Una aplicación de consola para pruebas y ejecución de scripts por lotes.

### Patrones de Diseño y Decisiones Arquitectónicas

Se aplicaron varios patrones de diseño para asegurar un código robusto, extensible y mantenible:

*   **MVVM (Model-View-ViewModel)**: Utilizado en el proyecto `PixelWallE.WPF` para separar la lógica de la interfaz de usuario (`View`) de la lógica de la aplicación y el estado (`ViewModel`). `MainViewModel.cs` centraliza el estado y las acciones, comunicándose con la `View` (`MainWindow.xaml`) a través de data binding y comandos.
*   **Visitor Pattern**: Es la piedra angular del compilador. La interfaz `IVisitor<T>` permite procesar el AST de diferentes maneras sin modificar las clases de los nodos (`Expr`, `Stmt`). Se implementa en:
    *   `CheckSemantic.cs`: Para realizar el análisis semántico.
    *   `Interpreter.cs`: Para ejecutar el código.
    *   `ASTPrinter.cs`: Una utilidad de depuración para visualizar el AST.
*   **Inyección de Dependencias y Servicios**: La `MainViewModel` no crea sus dependencias directamente, sino que las recibe a través de su constructor (`ICompilerService`, `IExecutionService`, `IFileService`). Esto desacopla la UI de los servicios de backend, facilitando las pruebas y la mantenibilidad.
*   **Strategy Pattern**: Los `ExecutionMode` (`Instant`, `StepByStep`, `PixelByPixel`) actúan como estrategias que modifican el comportamiento del `Interpreter`. El `ExecutionService` selecciona la estrategia y el `Interpreter` cambia su lógica de reporte de progreso y uso de `Task.Delay` en función del modo elegido.
*   **Result<T> para Manejo de Errores**: En lugar de usar excepciones para errores de compilación, se utiliza un tipo `Result<T>`. Este objeto encapsula un valor exitoso o una lista de errores (`IReadOnlyList<Error>`). Este enfoque, inspirado en la programación funcional, permite manejar los errores de forma más controlada y agregar múltiples errores en una sola pasada.

### Estructuras de Datos Fundamentales

Se crearon varias estructuras de datos a medida para modelar el dominio del problema:

*   **Jerarquía de Errores**: Se definió una clase base abstracta `Error` de la que heredan tipos de errores específicos: `LexicalError`, `SyntaxError`, `SemanticError` y `RuntimeError`. Cada uno almacena información relevante, como el `CodeLocation`, haciendo que los mensajes de error sean precisos y descriptivos.
*   `CodeLocation`: Un `readonly struct` que representa una ubicación exacta en el código fuente (línea y columna). Es inmutable y eficiente, y resulta fundamental para un buen reporte de errores.
*   `WallEColor`: Un `readonly struct` para representar colores. Su principal fortaleza es el método estático `TryParse`, que es capaz de interpretar una gran variedad de formatos de color: nombres predefinidos ("red", "blue"), y formatos hexadecimales como `#RGB`, `#RRGGBB`, `#ARGB` y `#AARRGGBB`.
*   `IntegerOrBool`: Una clase personalizada que unifica los tipos `int` y `bool`, imitando el comportamiento de lenguajes como C, donde un entero puede ser tratado como un booleano (0 es falso, no-cero es verdadero) y viceversa. Esto se logra de forma elegante mediante **operadores de conversión implícitos**.

### Tecnologías Clave

*   **.NET y C#**: Plataforma y lenguaje de desarrollo.
*   **WPF**: Framework para la interfaz gráfica de escritorio en Windows.
*   **SkiaSharp**: Biblioteca de gráficos 2D multiplataforma de alto rendimiento para el renderizado en el canvas.
*   **AvalonEdit**: Un componente de editor de texto basado en WPF con excelentes características.
*   **CommunityToolkit.Mvvm**: Librería para implementar el patrón MVVM de forma eficiente.

---

## 🚀 Uso

1.  Clonar el repositorio.
2.  Abrir la solución `PixelWallE.sln` en Visual Studio.
3.  Establecer `PixelWallE.WPF` como proyecto de inicio.
4.  Compilar y ejecutar el proyecto (pulsando F5).

---

## 👨‍💻 Autor

*   **Adrian Estevez Alvarez**

Este proyecto fue desarrollado para el curso de **Programación** en la Facultad de Matemática y Computación (MATCOM) de la Universidad de La Habana.