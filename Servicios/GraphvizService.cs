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
                string nombreLimpio = LimpiarNombreDot(hijo.Nombre);
                string padreLimpio = LimpiarNombreDot(nodo.Nombre);

                string color = hijo.EsHoja() ? "#C8E6C9" : "#BBDEFB";
                dot.AppendLine($"  \"{padreLimpio}\" -> \"{nombreLimpio}\";");
                dot.AppendLine($"  \"{nombreLimpio}\" [label=\"{hijo.Nombre}\\n({hijo.Libros.ObtenerCantidad()} libros)\", fillcolor=\"{color}\"];");

                GenerarDotCategoriasRecursivo(hijo, dot);
            }
        }

        /// <summary>
        /// Genera el código DOT de los libros de una categoría específica.
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
            dot.AppendLine($"  label=\"Libros de {categoria.Nombre} (orden ascendente por ISBN)\";");
            dot.AppendLine("  fontsize=18;");
            dot.AppendLine("  labelloc=t;");

            string catLimpia = LimpiarNombreDot(categoria.Nombre);
            dot.AppendLine($"  \"{catLimpia}\" [label=\"{categoria.Nombre}\", shape=box, fillcolor=\"#1565C0\", fontcolor=white, fontsize=14];");

            for (int i = 0; i < libros.Length; i++)
            {
                Libro libro = libros[i];
                string nodoLibro = $"libro_{libro.ISBN}";
                string label = $"ISBN: {libro.ISBN}\\n{libro.Titulo}\\n{libro.Autor}";

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
        /// </summary>
        public string GenerarDotArbolISBN()
        {
            ArbolLibrosISBN arbol = catalogo.ObtenerArbolISBN();

            if (arbol.ObtenerCantidad() == 0)
            {
                return GenerarDotInfo("El árbol de ISBN está vacío.");
            }

            StringBuilder dot = new StringBuilder();
            dot.AppendLine("digraph AVL_ISBN {");
            dot.AppendLine("  rankdir=TB;");
            dot.AppendLine("  node [shape=circle, style=filled, fontname=\"Arial\"];");
            dot.AppendLine("  edge [arrowsize=0.7];");
            dot.AppendLine("  label=\"Árbol AVL - ISBN\";");
            dot.AppendLine("  fontsize=18;");
            dot.AppendLine("  labelloc=t;");

            NodoISBN? raiz = arbol.ObtenerRaiz();

            if (raiz != null)
            {
                GenerarDotAvlISBNRecursivo(raiz, dot);
            }

            dot.AppendLine("}");

            return dot.ToString();
        }

        private void GenerarDotAvlISBNRecursivo(NodoISBN nodo, StringBuilder dot)
        {
            string nodoId = $"n{nodo.Libro.ISBN}";

            int alturaIzq = nodo.Izquierda?.Altura ?? 0;
            int alturaDer = nodo.Derecha?.Altura ?? 0;
            int equilibrio = alturaIzq - alturaDer;

            string color = (equilibrio > 1 || equilibrio < -1)
                ? "#EF9A9A"   // Rojo si desbalanceado
                : "#A5D6A7"; // Verde si equilibrado

            string tituloCorto = nodo.Libro.Titulo.Substring(0, Math.Min(15, nodo.Libro.Titulo.Length));
            string label = $"{nodo.Libro.ISBN}\\n{tituloCorto}";
            dot.AppendLine($"  \"{nodoId}\" [label=\"{label}\", fillcolor=\"{color}\"];");

            if (nodo.Izquierda != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"n{nodo.Izquierda.Libro.ISBN}\";");
                GenerarDotAvlISBNRecursivo(nodo.Izquierda, dot);
            }

            if (nodo.Derecha != null)
            {
                dot.AppendLine($"  \"{nodoId}\" -> \"n{nodo.Derecha.Libro.ISBN}\";");
                GenerarDotAvlISBNRecursivo(nodo.Derecha, dot);
            }
        }

        /// <summary>
        /// Genera DOT con un mensaje de error.
        /// </summary>
        private string GenerarDotError(string mensaje)
        {
            return $"digraph Error {{ node [shape=box, style=filled, fillcolor=\"#FFEBEE\", fontname=\"Arial\"]; error [label=\"{mensaje}\", fontcolor=\"#C62828\"]; }}";
        }

        /// <summary>
        /// Genera DOT con un mensaje informativo.
        /// </summary>
        private string GenerarDotInfo(string mensaje)
        {
            return $"digraph Info {{ node [shape=box, style=filled, fillcolor=\"#E8F5E9\", fontname=\"Arial\"]; info [label=\"{mensaje}\", fontcolor=\"#2E7D32\"]; }}";
        }

        // ===================== UTILIDADES =====================

        private string LimpiarNombreDot(string nombre)
        {
            return nombre.Replace("\"", "\\\"").Replace(" ", "_");
        }
    }
}
