using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    public class NodoISBN
    {
        public Libro Libro { get; set; }

        public NodoISBN? Izquierda { get; set; }
        public NodoISBN? Derecha { get; set; }

        public NodoISBN(Libro libro)
        {
            Libro = libro;
            Izquierda = null;
            Derecha = null;
        }
    }

    public class ArbolLibrosISBN
    {
        private NodoISBN? raiz;
        private int contador;

        public ArbolLibrosISBN()
        {
            raiz = null;
            contador = 0;
        }

        public void Insertar(Libro libro)
        {
            bool insertado = false;

            raiz = InsertarRecursivo(raiz, libro, ref insertado);

            if (insertado)
            {
                contador++;
            }
        }

        private NodoISBN InsertarRecursivo(
            NodoISBN? actual,
            Libro libro,
            ref bool insertado)
        {
            if (actual == null)
            {
                insertado = true;
                return new NodoISBN(libro);
            }

            if (libro.ISBN < actual.Libro.ISBN)
            {
                actual.Izquierda =
                    InsertarRecursivo(
                        actual.Izquierda,
                        libro,
                        ref insertado);
            }
            else if (libro.ISBN > actual.Libro.ISBN)
            {
                actual.Derecha =
                    InsertarRecursivo(
                        actual.Derecha,
                        libro,
                        ref insertado);
            }

            return actual;
        }

        public Libro? Buscar(int isbn)
        {
            NodoISBN? actual = raiz;

            while (actual != null)
            {
                if (isbn == actual.Libro.ISBN)
                {
                    return actual.Libro;
                }

                if (isbn < actual.Libro.ISBN)
                {
                    actual = actual.Izquierda;
                }
                else
                {
                    actual = actual.Derecha;
                }
            }

            return null;
        }

        public bool Eliminar(int isbn)
        {
            bool eliminado = false;

            raiz = EliminarRecursivo(
                raiz,
                isbn,
                ref eliminado);

            if (eliminado)
            {
                contador--;
            }

            return eliminado;
        }

        private NodoISBN? EliminarRecursivo(
            NodoISBN? actual,
            int isbn,
            ref bool eliminado)
        {
            if (actual == null)
            {
                return null;
            }

            if (isbn < actual.Libro.ISBN)
            {
                actual.Izquierda =
                    EliminarRecursivo(
                        actual.Izquierda,
                        isbn,
                        ref eliminado);

                return actual;
            }

            if (isbn > actual.Libro.ISBN)
            {
                actual.Derecha =
                    EliminarRecursivo(
                        actual.Derecha,
                        isbn,
                        ref eliminado);

                return actual;
            }

            eliminado = true;

            if (actual.Izquierda == null)
            {
                return actual.Derecha;
            }

            if (actual.Derecha == null)
            {
                return actual.Izquierda;
            }

            NodoISBN sucesor =
                ObtenerMinimoNodo(actual.Derecha);

            actual.Libro = sucesor.Libro;

            bool eliminadoSucesor = false;

            actual.Derecha =
                EliminarRecursivo(
                    actual.Derecha,
                    sucesor.Libro.ISBN,
                    ref eliminadoSucesor);

            return actual;
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

        public Libro? ObtenerMenorISBN()
        {
            if (raiz == null)
            {
                return null;
            }

            return ObtenerMinimoNodo(raiz).Libro;
        }

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

        public int ObtenerCantidad()
        {
            return contador;
        }

        public Libro[] ObtenerAscendente()
        {
            Libro[] libros = new Libro[contador];
            int posicion = 0;

            InOrden(raiz, libros, ref posicion);

            return libros;
        }

        private void InOrden(
            NodoISBN? actual,
            Libro[] libros,
            ref int posicion)
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

        public Libro[] ObtenerDescendente()
        {
            Libro[] libros = new Libro[contador];
            int posicion = 0;

            InOrdenInverso(raiz, libros, ref posicion);

            return libros;
        }

        private void InOrdenInverso(
            NodoISBN? actual,
            Libro[] libros,
            ref int posicion)
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

        public NodoISBN? ObtenerRaiz()
        {
            return raiz;
        }
    }
}
