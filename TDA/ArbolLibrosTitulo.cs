using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Nodo del árbol AVL indexado por Título.
    /// Permite búsqueda y listado alfabético de libros.
    /// </summary>
    public class NodoTitulo
    {
        public Libro Libro { get; set; }
        public NodoTitulo? Izquierda { get; set; }
        public NodoTitulo? Derecha { get; set; }
        public int Altura { get; set; }

        public NodoTitulo(Libro libro)
        {
            Libro = libro;
            Izquierda = null;
            Derecha = null;
            Altura = 1;
        }
    }

    /// <summary>
    /// Árbol AVL para libros indexados por Título (orden alfabético).
    /// La comparación se realiza ignorando mayúsculas/minúsculas para búsquedas más naturales.
    /// Garantiza O(log n) en operaciones principales.
    /// </summary>
    public class ArbolLibrosTitulo
    {
        private NodoTitulo? raiz;
        private int contador;

        public ArbolLibrosTitulo()
        {
            raiz = null;
            contador = 0;
        }

        // ===================== UTILIDADES DE ALTURA =====================

        private int ObtenerAltura(NodoTitulo? nodo)
        {
            return nodo == null ? 0 : nodo.Altura;
        }

        private int ObtenerEquilibrio(NodoTitulo? nodo)
        {
            return nodo == null ? 0 : ObtenerAltura(nodo.Izquierda) - ObtenerAltura(nodo.Derecha);
        }

        private void ActualizarAltura(NodoTitulo nodo)
        {
            int altIzq = ObtenerAltura(nodo.Izquierda);
            int altDer = ObtenerAltura(nodo.Derecha);
            nodo.Altura = 1 + (altIzq > altDer ? altIzq : altDer);
        }

        // ===================== ROTACIONES =====================

        private NodoTitulo RotarDerecha(NodoTitulo y)
        {
            NodoTitulo x = y.Izquierda!;
            NodoTitulo B = x.Derecha!;

            x.Derecha = y;
            y.Izquierda = B;

            ActualizarAltura(y);
            ActualizarAltura(x);

            return x;
        }

        private NodoTitulo RotarIzquierda(NodoTitulo x)
        {
            NodoTitulo y = x.Derecha!;
            NodoTitulo B = y.Izquierda!;

            y.Izquierda = x;
            x.Derecha = B;

            ActualizarAltura(x);
            ActualizarAltura(y);

            return y;
        }

        // ===================== COMPARACIÓN =====================

        /// <summary>
        /// Compara dos títulos ignorando mayúsculas/minúsculas.
        /// Retorna negativo si a < b, 0 si son iguales, positivo si a > b.
        /// </summary>
        private int CompararTitulos(string a, string b)
        {
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // ===================== INSERCIÓN =====================

        /// <summary>
        /// Inserta un libro en el árbol por título.
        /// Si dos libros tienen el mismo título, se diferencia por ISBN.
        /// Retorna false si ya existe un libro con el mismo título E ISBN.
        /// </summary>
        public bool Insertar(Libro libro)
        {
            bool insertado = false;
            raiz = InsertarRecursivo(raiz, libro, ref insertado);

            if (insertado)
            {
                contador++;
            }

            return insertado;
        }

        private NodoTitulo InsertarRecursivo(NodoTitulo? actual, Libro libro, ref bool insertado)
        {
            if (actual == null)
            {
                insertado = true;
                return new NodoTitulo(libro);
            }

            int comparacion = CompararTitulos(libro.Titulo, actual.Libro.Titulo);

            if (comparacion < 0)
            {
                actual.Izquierda = InsertarRecursivo(actual.Izquierda, libro, ref insertado);
            }
            else if (comparacion > 0)
            {
                actual.Derecha = InsertarRecursivo(actual.Derecha, libro, ref insertado);
            }
            else
            {
                // Títulos iguales → comparar por ISBN para permitir libros diferentes
                if (libro.ISBN < actual.Libro.ISBN)
                {
                    actual.Izquierda = InsertarRecursivo(actual.Izquierda, libro, ref insertado);
                }
                else if (libro.ISBN > actual.Libro.ISBN)
                {
                    actual.Derecha = InsertarRecursivo(actual.Derecha, libro, ref insertado);
                }
                // Mismo título y mismo ISBN = duplicado, no se inserta
            }

            ActualizarAltura(actual);
            return Balancear(actual);
        }

        // ===================== ELIMINACIÓN =====================

        public bool Eliminar(int isbn)
        {
            bool eliminado = false;
            raiz = EliminarRecursivo(raiz, isbn, ref eliminado);

            if (eliminado)
            {
                contador--;
            }

            return eliminado;
        }

        private NodoTitulo? EliminarRecursivo(NodoTitulo? actual, int isbn, ref bool eliminado)
        {
            if (actual == null)
            {
                return null;
            }

            int comparacion = isbn.CompareTo(actual.Libro.ISBN);

            if (comparacion < 0)
            {
                // Buscar en subárbol izquierdo (por ISBN para encontrar el nodo exacto)
                actual.Izquierda = EliminarRecursivo(actual.Izquierda, isbn, ref eliminado);
            }
            else if (comparacion > 0)
            {
                actual.Derecha = EliminarRecursivo(actual.Derecha, isbn, ref eliminado);
            }
            else
            {
                // Nodo encontrado por ISBN
                eliminado = true;

                if (actual.Izquierda == null)
                {
                    return actual.Derecha;
                }
                if (actual.Derecha == null)
                {
                    return actual.Izquierda;
                }

                // Dos hijos → sucesor inorden
                NodoTitulo sucesor = ObtenerMinimoNodo(actual.Derecha);
                actual.Libro = sucesor.Libro;
                actual.Derecha = EliminarRecursivo(actual.Derecha, sucesor.Libro.ISBN, ref eliminado);
            }

            ActualizarAltura(actual);
            return Balancear(actual);
        }

        // ===================== BALANCEO =====================

        private NodoTitulo Balancear(NodoTitulo nodo)
        {
            int equilibrio = ObtenerEquilibrio(nodo);

            if (equilibrio > 1)
            {
                if (ObtenerEquilibrio(nodo.Izquierda) < 0)
                {
                    nodo.Izquierda = RotarIzquierda(nodo.Izquierda!);
                }
                return RotarDerecha(nodo);
            }

            if (equilibrio < -1)
            {
                if (ObtenerEquilibrio(nodo.Derecha) > 0)
                {
                    nodo.Derecha = RotarDerecha(nodo.Derecha!);
                }
                return RotarIzquierda(nodo);
            }

            return nodo;
        }

        // ===================== BÚSQUEDA =====================

        /// <summary>
        /// Busca un libro por ISBN en este árbol.
        /// </summary>
        public Libro? BuscarPorISBN(int isbn)
        {
            NodoTitulo? actual = raiz;

            while (actual != null)
            {
                if (isbn == actual.Libro.ISBN)
                {
                    return actual.Libro;
                }

                actual = isbn < actual.Libro.ISBN
                    ? actual.Izquierda
                    : actual.Derecha;
            }

            return null;
        }

        /// <summary>
        /// Busca un libro por título exacto (ignorando mayúsculas).
        /// Retorna el primer libro encontrado con ese título.
        /// </summary>
        public Libro? BuscarPorTitulo(string titulo)
        {
            NodoTitulo? actual = raiz;

            while (actual != null)
            {
                int comparacion = CompararTitulos(titulo, actual.Libro.Titulo);

                if (comparacion == 0)
                {
                    return actual.Libro;
                }

                actual = comparacion < 0
                    ? actual.Izquierda
                    : actual.Derecha;
            }

            return null;
        }

        // ===================== MÍNIMO Y MÁXIMO =====================

        private NodoTitulo ObtenerMinimoNodo(NodoTitulo nodo)
        {
            NodoTitulo actual = nodo;
            while (actual.Izquierda != null)
            {
                actual = actual.Izquierda;
            }
            return actual;
        }

        public Libro? ObtenerPrimeroAlfabeticamente()
        {
            return raiz == null ? null : ObtenerMinimoNodo(raiz).Libro;
        }

        public Libro? ObtenerUltimoAlfabeticamente()
        {
            if (raiz == null)
            {
                return null;
            }

            NodoTitulo actual = raiz;
            while (actual.Derecha != null)
            {
                actual = actual.Derecha;
            }
            return actual.Libro;
        }

        // ===================== RECORRIDOS =====================

        /// <summary>
        /// Retorna todos los libros ordenados alfabéticamente por título.
        /// </summary>
        public Libro[] ObtenerOrdenAlfabetico()
        {
            Libro[] libros = new Libro[contador];
            int posicion = 0;
            InOrden(raiz, libros, ref posicion);
            return libros;
        }

        private void InOrden(NodoTitulo? actual, Libro[] libros, ref int posicion)
        {
            if (actual == null)
            {
                return;
            }

            InOrden(actual.Izquierda, libros, ref posicion);
            libros[posicion] = actual.Libro;
            posicion++;
            InOrden(actual.Derecha, libros, ref posicion);
        }

        // ===================== UTILIDADES =====================

        public int ObtenerCantidad()
        {
            return contador;
        }

        public NodoTitulo? ObtenerRaiz()
        {
            return raiz;
        }

        /// <summary>
        /// Genera representación en texto del árbol (para depuración).
        /// </summary>
        public string GenerarTextoArbol()
        {
            if (raiz == null)
            {
                return "(árbol vacío)";
            }

            return GenerarTextoNodo(raiz, "", true);
        }

        private string GenerarTextoNodo(NodoTitulo nodo, string prefijo, bool esUltimo)
        {
            string resultado = prefijo + (esUltimo ? "└── " : "├── ")
                + $"[{nodo.Libro.ISBN}] {nodo.Libro.Titulo} (h={nodo.Altura})\n";

            string nuevoPrefijo = prefijo + (esUltimo ? "    " : "│   ");

            if (nodo.Izquierda != null)
            {
                resultado += GenerarTextoNodo(nodo.Izquierda, nuevoPrefijo, nodo.Derecha == null);
            }
            if (nodo.Derecha != null)
            {
                resultado += GenerarTextoNodo(nodo.Derecha, nuevoPrefijo, true);
            }

            return resultado;
        }
    }
}
