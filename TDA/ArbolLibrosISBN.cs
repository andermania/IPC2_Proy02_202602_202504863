using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Nodo del árbol AVL indexado por ISBN.
    /// Cada nodo almacena un libro y mantiene la altura para balanceo.
    /// </summary>
    public class NodoISBN
    {
        public Libro Libro { get; set; }
        public NodoISBN? Izquierda { get; set; }
        public NodoISBN? Derecha { get; set; }
        public int Altura { get; set; }

        public NodoISBN(Libro libro)
        {
            Libro = libro;
            Izquierda = null;
            Derecha = null;
            Altura = 1; // Nuevo nodo siempre tiene altura 1
        }
    }

    /// <summary>
    /// Árbol AVL (Adelson-Velsky y Landis) para libros indexados por ISBN.
    /// Garantiza O(log n) en inserción, búsqueda y eliminación mediante rotaciones.
    /// No utiliza estructuras de C# (List, Dictionary, etc.).
    /// </summary>
    public class ArbolLibrosISBN
    {
        private NodoISBN? raiz;
        private int contador;

        public ArbolLibrosISBN()
        {
            raiz = null;
            contador = 0;
        }

        // ===================== UTILIDADES DE ALTURA =====================

        /// <summary>
        /// Retorna la altura de un nodo (0 si es null).
        /// </summary>
        private int ObtenerAltura(NodoISBN? nodo)
        {
            return nodo == null ? 0 : nodo.Altura;
        }

        /// <summary>
        /// Calcula el factor de equilibrio de un nodo.
        /// positivo = subárbol izquierdo más pesado
        /// negativo = subárbol derecho más pesado
        /// 0 = equilibrado
        /// </summary>
        private int ObtenerEquilibrio(NodoISBN? nodo)
        {
            return nodo == null ? 0 : ObtenerAltura(nodo.Izquierda) - ObtenerAltura(nodo.Derecha);
        }

        /// <summary>
        /// Recalcula la altura de un nodo basándose en sus hijos.
        /// </summary>
        private void ActualizarAltura(NodoISBN nodo)
        {
            int altIzq = ObtenerAltura(nodo.Izquierda);
            int altDer = ObtenerAltura(nodo.Derecha);
            nodo.Altura = 1 + (altIzq > altDer ? altIzq : altDer);
        }

        // ===================== ROTACIONES =====================

        /// <summary>
        /// Rotación simple a la derecha.
        ///      y           x
        ///     / \         / \
        ///    x   C  →    A   y
        ///   / \             / \
        ///  A   B           B   C
        /// </summary>
        private NodoISBN RotarDerecha(NodoISBN y)
        {
            NodoISBN x = y.Izquierda!;
            NodoISBN? B = x.Derecha;

            x.Derecha = y;
            y.Izquierda = B;

            ActualizarAltura(y);
            ActualizarAltura(x);

            return x;
        }

        /// <summary>
        /// Rotación simple a la izquierda.
        ///    x               y
        ///   / \             / \
        ///  A   y     →     x   C
        ///     / \         / \
        ///    B   C       A   B
        /// </summary>
        private NodoISBN RotarIzquierda(NodoISBN x)
        {
            NodoISBN y = x.Derecha!;
            NodoISBN? B = y.Izquierda;

            y.Izquierda = x;
            x.Derecha = B;

            ActualizarAltura(x);
            ActualizarAltura(y);

            return y;
        }

        // ===================== INSERCIÓN =====================

        /// <summary>
        /// Inserta un libro en el árbol AVL por ISBN.
        /// Retorna false si el ISBN ya existe.
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

        private NodoISBN InsertarRecursivo(NodoISBN? actual, Libro libro, ref bool insertado)
        {
            if (actual == null)
            {
                insertado = true;
                return new NodoISBN(libro);
            }

            if (libro.ISBN < actual.Libro.ISBN)
            {
                actual.Izquierda = InsertarRecursivo(actual.Izquierda, libro, ref insertado);
            }
            else if (libro.ISBN > actual.Libro.ISBN)
            {
                actual.Derecha = InsertarRecursivo(actual.Derecha, libro, ref insertado);
            }
            // Si ISBN == actual.ISBN, no se inserta (duplicado)
            else
            {
                return actual;
            }

            // Actualizar altura y aplicar rotaciones si es necesario
            ActualizarAltura(actual);
            return Balancear(actual);
        }

        // ===================== ELIMINACIÓN =====================

        /// <summary>
        /// Elimina un libro del árbol por ISBN.
        /// Retorna false si no se encontró.
        /// </summary>
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

        private NodoISBN? EliminarRecursivo(NodoISBN? actual, int isbn, ref bool eliminado)
        {
            if (actual == null)
            {
                return null;
            }

            if (isbn < actual.Libro.ISBN)
            {
                actual.Izquierda = EliminarRecursivo(actual.Izquierda, isbn, ref eliminado);
            }
            else if (isbn > actual.Libro.ISBN)
            {
                actual.Derecha = EliminarRecursivo(actual.Derecha, isbn, ref eliminado);
            }
            else
            {
                // Nodo encontrado
                eliminado = true;

                // Caso 1: Nodo hoja o con un solo hijo
                if (actual.Izquierda == null)
                {
                    return actual.Derecha;
                }
                if (actual.Derecha == null)
                {
                    return actual.Izquierda;
                }

                // Caso 2: Dos hijos → reemplazar con sucesor inorden (mínimo del subárbol derecho)
                NodoISBN sucesor = ObtenerMinimoNodo(actual.Derecha);
                actual.Libro = sucesor.Libro;
                actual.Derecha = EliminarRecursivo(actual.Derecha, sucesor.Libro.ISBN, ref eliminado);
            }

            ActualizarAltura(actual);
            return Balancear(actual);
        }

        // ===================== BALANCEO GENÉRICO =====================

        /// <summary>
        /// Verifica el equilibrio de un nodo y aplica las rotaciones necesarias.
        /// Casos: II, DD, ID, DI
        /// </summary>
        private NodoISBN Balancear(NodoISBN nodo)
        {
            int equilibrio = ObtenerEquilibrio(nodo);

            // Subárbol izquierdo pesado (casos II e ID)
            if (equilibrio > 1)
            {
                if (ObtenerEquilibrio(nodo.Izquierda) < 0)
                {
                    // Caso ID: rotación doble izquierda-derecha
                    nodo.Izquierda = RotarIzquierda(nodo.Izquierda!);
                }
                // Caso II: rotación simple derecha
                return RotarDerecha(nodo);
            }

            // Subárbol derecho pesado (casos DD e DI)
            if (equilibrio < -1)
            {
                if (ObtenerEquilibrio(nodo.Derecha) > 0)
                {
                    // Caso DI: rotación doble derecha-izquierda
                    nodo.Derecha = RotarDerecha(nodo.Derecha!);
                }
                // Caso DD: rotación simple izquierda
                return RotarIzquierda(nodo);
            }

            return nodo; // Ya está equilibrado
        }

        // ===================== BÚSQUEDA =====================

        /// <summary>
        /// Busca un libro por ISBN. O(log n).
        /// </summary>
        public Libro? Buscar(int isbn)
        {
            NodoISBN? actual = raiz;

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

        // ===================== MÍNIMO Y MÁXIMO =====================

        /// <summary>
        /// Obtiene el libro con el ISBN más bajo.
        /// </summary>
        public Libro? ObtenerMenorISBN()
        {
            return raiz == null ? null : ObtenerMinimoNodo(raiz).Libro;
        }

        /// <summary>
        /// Obtiene el libro con el ISBN más alto.
        /// </summary>
        public Libro? ObtenerMayorISBN()
        {
            if (raiz == null)
            {
                return null;
            }

            NodoISBN actual = raiz;
            while (actual.Derecha != null)
            {
                actual = actual.Derecha;
            }
            return actual.Libro;
        }

        private NodoISBN ObtenerMinimoNodo(NodoISBN nodo)
        {
            NodoISBN actual = nodo;
            while (actual.Izquierda != null)
            {
                actual = actual.Izquierda;
            }
            return actual;
        }

        // ===================== RECORRIDOS =====================

        /// <summary>
        /// Recorrido inorden ascendente (menor a mayor ISBN).
        /// </summary>
        public Libro[] ObtenerAscendente()
        {
            Libro[] libros = new Libro[contador];
            int posicion = 0;
            InOrden(raiz, libros, ref posicion);
            return libros;
        }

        private void InOrden(NodoISBN? actual, Libro[] libros, ref int posicion)
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

        /// <summary>
        /// Recorrido inorden descendente (mayor a menor ISBN).
        /// </summary>
        public Libro[] ObtenerDescendente()
        {
            Libro[] libros = new Libro[contador];
            int posicion = 0;
            InOrdenInverso(raiz, libros, ref posicion);
            return libros;
        }

        private void InOrdenInverso(NodoISBN? actual, Libro[] libros, ref int posicion)
        {
            if (actual == null)
            {
                return;
            }

            InOrdenInverso(actual.Derecha, libros, ref posicion);
            libros[posicion] = actual.Libro;
            posicion++;
            InOrdenInverso(actual.Izquierda, libros, ref posicion);
        }

        // ===================== UTILIDADES =====================

        public int ObtenerCantidad()
        {
            return contador;
        }

        public NodoISBN? ObtenerRaiz()
        {
            return raiz;
        }

        /// <summary>
        /// Genera una representación en texto del árbol (para depuración).
        /// Muestra la estructura con indentación.
        /// </summary>
        public string GenerarTextoArbol()
        {
            if (raiz == null)
            {
                return "(árbol vacío)";
            }

            return GenerarTextoNodo(raiz, "", true);
        }

        private string GenerarTextoNodo(NodoISBN nodo, string prefijo, bool esUltimo)
        {
            string resultado = prefijo + (esUltimo ? "└── " : "├── ")
                + $"[{nodo.Libro.ISBN}] {nodo.Libro.Titulo} (h={nodo.Altura})\n";

            string nuevoPrefijo = prefijo + (esUltimo ? "    " : "│   ");

            int hijos = 0;
            if (nodo.Izquierda != null) hijos++;
            if (nodo.Derecha != null) hijos++;

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
