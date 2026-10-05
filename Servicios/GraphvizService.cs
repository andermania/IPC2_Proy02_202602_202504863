using System.Text;
using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.TDA;

namespace GestiónDeBiblioteca.Servicios
{
    /// <summary>
    /// Servicio para generar código DOT de Graphviz del catálogo de la biblioteca.
    /// El código DOT se envía al navegador donde se renderiza con viz.js.
    /// No requiere Graphviz instalado en el servidor.
    /// </summary>
    public class GraphvizService
    {
        private readonly CatalogoController catalogo;

        public GraphvizService(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
        }

        /// <summary>
        /// Genera el código DOT de la estructura de categorías.
        /// </summary>
        public string GenerarDotCategorias()
        {
            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph Categorias {");
            dot.AppendLine("  rankdir=TB;");
            dot.AppendLine("  node [shape=box, style=filled, fillcolor=\"#E3F2FD\", fontname=\"Arial\"];");
            dot.AppendLine("  edge [color=\"#666666\"];");
            dot.AppendLine("  label=\"Estructura de Categorías\";");
            dot.AppendLine("  fontsize=20;");
            dot.AppendLine("  labelloc=t;");

            Categoria raiz = catalogo.ObtenerArbolCategorias().ObtenerRaiz();
            dot.AppendLine($"  \"{LimpiarNombreDot(raiz.Nombre)}\" [fillcolor=\"#1565C0\", fontcolor=white];");

            GenerarDotCategoriasRecursivo(raiz, dot);

            dot.AppendLine("}");

            return dot.ToString();
        }

        private void GenerarDotCategoriasRecursivo(Categoria nodo, StringBuilder dot)
        {
            Categoria[] hijos = nodo.Hijos.ObtenerTodas();

            for (int i = 0; i < hijos.Length; i++)
            {
                Categoria hijo = hijos[i];
                // ID único por ruta completa (evita colisiones); etiqueta legible escapada
                string idHijo = EscaparEtiquetaDot(hijo.ObtenerRutaCompleta());
                string idPadre = EscaparEtiquetaDot(nodo == catalogo.ObtenerArbolCategorias().ObtenerRaiz() ? nodo.Nombre : nodo.ObtenerRutaCompleta());
                string etiquetaHijo = EscaparEtiquetaDot(hijo.Nombre);

                string color = hijo.EsHoja() ? "#C8E6C9" : "#BBDEFB";
                dot.AppendLine($"  \"{idPadre}\" -> \"{idHijo}\";");
                dot.AppendLine($"  \"{idHijo}\" [label=\"{etiquetaHijo}\\n({hijo.Libros.ObtenerCantidad()} libros)\", fillcolor=\"{color}\"];");

                GenerarDotCategoriasRecursivo(hijo, dot);
            }
        }

        /// <summary>
        /// Genera el código DOT del subárbol de categorías desde una ruta específica.
        /// Cubre el requisito de visualizar desde una subcategoría determinada.
        /// </summary>
        public string GenerarDotCategoriasDesde(string ruta)
        {
            Categoria? desde = catalogo.BuscarCategoria(ruta);

            if (desde == null)
            {
                desde = catalogo.BuscarCategoriaPorNombre(ruta);
            }

            if (desde == null)
            {
                return GenerarDotError("Categoría no encontrada: " + ruta);
            }

            StringBuilder sub = new StringBuilder();
            sub.AppendLine("digraph CategoriasDesde {");
            sub.AppendLine("  rankdir=TB;");
            sub.AppendLine("  node [shape=box, style=filled, fillcolor=\"#E3F2FD\", fontname=\"Arial\"];");
            sub.AppendLine("  edge [color=\"#666666\"];");
            sub.AppendLine($"  label=\"Estructura desde {EscaparEtiquetaDot(desde.Nombre)}\";");
            sub.AppendLine("  fontsize=20;");
            sub.AppendLine("  labelloc=t;");

            string idDesde = EscaparEtiquetaDot(desde.ObtenerRutaCompleta());
            sub.AppendLine($"  \"{idDesde}\" [label=\"{EscaparEtiquetaDot(desde.Nombre)}\\n({desde.Libros.ObtenerCantidad()} libros)\", fillcolor=\"#1565C0\", fontcolor=white];");

            GenerarDotCategoriasRecursivo(desde, sub);

            sub.AppendLine("}");

            return sub.ToString();
        }

        /// <summary>
        /// Genera el código DOT de los libros de una categoría específica,
        /// en orden ascendente por ISBN (recorrido inorden del AVL de la categoría).
        /// </summary>
        public string GenerarDotLibrosCategoria(string rutaCategoria)
        {
            Categoria? categoria = catalogo.BuscarCategoria(rutaCategoria);

            if (categoria == null)
            {
                return GenerarDotError("Categoría no encontrada: " + rutaCategoria);
            }

            Libro[] libros = categoria.Libros.ObtenerAscendente();

            if (libros.Length == 0)
            {
                return GenerarDotInfo("La categoría \"" + categoria.Nombre + "\" no tiene libros.");
            }

            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph Libros {");
            dot.AppendLine("  rankdir=TB;");
            dot.AppendLine("  node [shape=record, style=filled, fontname=\"Arial\"];");
            dot.AppendLine($"  label=\"Libros de {EscaparEtiquetaDot(categoria.Nombre)} (orden ascendente por ISBN)\";");
            dot.AppendLine("  fontsize=18;");
            dot.AppendLine("  labelloc=t;");

            string catLimpia = EscaparEtiquetaDot(categoria.ObtenerRutaCompleta());
            dot.AppendLine($"  \"{catLimpia}\" [label=\"{EscaparEtiquetaDot(categoria.Nombre)}\", shape=box, fillcolor=\"#1565C0\", fontcolor=white, fontsize=14];");

            for (int i = 0; i < libros.Length; i++)
            {
                Libro libro = libros[i];
                string nodoLibro = $"libro_{libro.ISBN}";
                string label = $"ISBN: {libro.ISBN}\\n{EscaparEtiquetaDot(libro.Titulo)}\\n{EscaparEtiquetaDot(libro.Autor)}";

                dot.AppendLine($"  \"{nodoLibro}\" [label=\"{label}\", fillcolor=\"#FFF9C4\"];");

                if (i == 0)
                {
                    dot.AppendLine($"  \"{catLimpia}\" -> \"{nodoLibro}\";");
                }
                else
                {
                    string nodoAnterior = $"libro_{libros[i - 1].ISBN}";
                    dot.AppendLine($"  \"{nodoAnterior}\" -> \"{nodoLibro}\";");
                }
            }

            dot.AppendLine("}");

            return dot.ToString();
        }

        /// <summary>
        /// Genera el código DOT del árbol AVL de ISBN.
        /// Por rendimiento del navegador (viz.js), solo dibuja los niveles
        /// superiores cuando el árbol es grande (ver MaxNivelesDiagrama).
        /// </summary>
        // Niveles máximos dibujados en diagramas AVL (0 = raíz).
        /// 7 niveles = hasta 255 nodos: viz.js lo renderiza sin colgarse
        /// incluso con catálogos de 10 000 libros.
        /// </summary>
        private const int MaxNivelesDiagrama = 7;

        private const int MaxNodosDiagrama = (1 << (MaxNivelesDiagrama + 1)) - 1; // 255

        public string GenerarDotArbolISBN()
        {
            ArbolLibrosISBN arbol = catalogo.ObtenerArbolISBN();

            if (arbol.ObtenerCantidad() == 0)
            {
                return GenerarDotInfo("El árbol de ISBN está vacío.");
            }

            string sufijo = arbol.ObtenerCantidad() > MaxNodosDiagrama
                ? $" ({arbol.ObtenerCantidad()} libros; se muestran los {MaxNivelesDiagrama + 1} niveles superiores)"
                : $" ({arbol.ObtenerCantidad()} libros)";

            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph AVL_ISBN {");
            dot.AppendLine("  rankdir=TB;");
            dot.AppendLine("  node [shape=circle, style=filled, fontname=\"Arial\"];");
            dot.AppendLine("  edge [arrowsize=0.7];");
            dot.AppendLine($"  label=\"Arbol AVL - ISBN{sufijo}\";");
            dot.AppendLine("  fontsize=18;");
            dot.AppendLine("  labelloc=t;");

            NodoISBN? raiz = arbol.ObtenerRaiz();

            if (raiz != null)
            {
                GenerarDotAvlISBNRecursivo(raiz, dot, 0);
            }

            dot.AppendLine("}");

            return dot.ToString();
        }

        private void GenerarDotAvlISBNRecursivo(NodoISBN nodo, StringBuilder dot, int nivel)
        {
            string nodoId = $"n{nodo.Libro.ISBN}";

            int alturaIzq = nodo.Izquierda?.Altura ?? 0;
            int alturaDer = nodo.Derecha?.Altura ?? 0;
            int equilibrio = alturaIzq - alturaDer;

            string color = (equilibrio > 1 || equilibrio < -1)
                ? "#EF9A9A"   // Rojo si desbalanceado
                : "#A5D6A7"; // Verde si equilibrado

            string tituloCorto = nodo.Libro.Titulo.Substring(0, Math.Min(15, nodo.Libro.Titulo.Length));
            string label = $"{nodo.Libro.ISBN}\\n{EscaparEtiquetaDot(tituloCorto)}";
            dot.AppendLine($"  \"{nodoId}\" [label=\"{label}\", fillcolor=\"{color}\"];");

            if (nivel >= MaxNivelesDiagrama)
            {
                return;
            }

            if (nodo.Izquierda != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"n{nodo.Izquierda.Libro.ISBN}\";");
                GenerarDotAvlISBNRecursivo(nodo.Izquierda, dot, nivel + 1);
            }

            if (nodo.Derecha != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"n{nodo.Derecha.Libro.ISBN}\";");
                GenerarDotAvlISBNRecursivo(nodo.Derecha, dot, nivel + 1);
            }
        }

        /// <summary>
        /// Genera el código DOT del árbol AVL alfabético (por Título).
        /// Misma estructura y rotaciones que el AVL de ISBN, pero ordenado
        /// con comparación insensible a mayúsculas (ver ArbolLibrosTitulo).
        /// También limitado a los niveles superiores por rendimiento.
        /// </summary>
        public string GenerarDotArbolTitulo()
        {
            ArbolLibrosTitulo arbol = catalogo.ObtenerArbolTitulo();

            if (arbol.ObtenerCantidad() == 0)
            {
                return GenerarDotInfo("El árbol alfabético está vacío.");
            }

            string sufijo = arbol.ObtenerCantidad() > MaxNodosDiagrama
                ? $" ({arbol.ObtenerCantidad()} libros; se muestran los {MaxNivelesDiagrama + 1} niveles superiores)"
                : $" ({arbol.ObtenerCantidad()} libros)";

            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph AVL_Titulo {");
            dot.AppendLine("  rankdir=TB;");
            dot.AppendLine("  node [shape=box, style=\"rounded,filled\", fontname=\"Arial\"];");
            dot.AppendLine("  edge [arrowsize=0.7];");
            dot.AppendLine($"  label=\"Arbol AVL - Titulo (orden alfabetico){sufijo}\";");
            dot.AppendLine("  fontsize=18;");
            dot.AppendLine("  labelloc=t;");

            NodoTitulo? raiz = arbol.ObtenerRaiz();

            if (raiz != null)
            {
                GenerarDotAvlTituloRecursivo(raiz, dot, 0);
            }

            dot.AppendLine("}");

            return dot.ToString();
        }

        private void GenerarDotAvlTituloRecursivo(NodoTitulo nodo, StringBuilder dot, int nivel)
        {
            string nodoId = $"t{nodo.Libro.ISBN}";

            int alturaIzq = nodo.Izquierda?.Altura ?? 0;
            int alturaDer = nodo.Derecha?.Altura ?? 0;
            int equilibrio = alturaIzq - alturaDer;

            string color = (equilibrio > 1 || equilibrio < -1)
                ? "#EF9A9A"   // Rojo si desbalanceado
                : "#FFE0B2"; // Naranja claro si equilibrado (difiere del ISBN)

            string tituloCorto = nodo.Libro.Titulo.Substring(0, Math.Min(18, nodo.Libro.Titulo.Length));
            string label = $"{EscaparEtiquetaDot(tituloCorto)}\\n[{nodo.Libro.ISBN}]";
            dot.AppendLine($"  \"{nodoId}\" [label=\"{label}\", fillcolor=\"{color}\"];");

            if (nivel >= MaxNivelesDiagrama)
            {
                return;
            }

            if (nodo.Izquierda != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"t{nodo.Izquierda.Libro.ISBN}\";");
                GenerarDotAvlTituloRecursivo(nodo.Izquierda, dot, nivel + 1);
            }

            if (nodo.Derecha != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"t{nodo.Derecha.Libro.ISBN}\";");
                GenerarDotAvlTituloRecursivo(nodo.Derecha, dot, nivel + 1);
            }
        }

        /// <summary>
        /// Genera DOT con un mensaje de error.
        /// </summary>
        private string GenerarDotError(string mensaje)
        {
            return $"digraph Error {{ node [shape=box, style=filled, fillcolor=\"#FFEBEE\", fontname=\"Arial\"]; error [label=\"{EscaparEtiquetaDot(mensaje)}\", fontcolor=\"#C62828\"]; }}";
        }

        /// <summary>
        /// Genera DOT con un mensaje informativo.
        /// </summary>
        private string GenerarDotInfo(string mensaje)
        {
            return $"digraph Info {{ node [shape=box, style=filled, fillcolor=\"#E8F5E9\", fontname=\"Arial\"]; info [label=\"{EscaparEtiquetaDot(mensaje)}\", fontcolor=\"#2E7D32\"]; }}";
        }

        // ===================== UTILIDADES =====================

        private string LimpiarNombreDot(string nombre)
        {
            if (nombre == null)
            {
                return "";
            }

            // Escapar para etiquetas DOT entre comillas: barra, comillas, saltos
            return nombre
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", "\\n");
        }

        private string EscaparEtiquetaDot(string texto)
        {
            return LimpiarNombreDot(texto);
        }
    }
}
