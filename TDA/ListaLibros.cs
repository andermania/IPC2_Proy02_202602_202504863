using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    public class NodoLibro
    {
        public Libro Libro { get; set; }
        public NodoLibro? Siguiente { get; set; }

        public NodoLibro(Libro libro)
        {
            Libro = libro;
            Siguiente = null;
        }
    }

    public class ListaLibros
    {
        private NodoLibro? primero;
        private NodoLibro? ultimo;

        public int contador = 0;

        public bool EstaVacia()
        {
            return primero == null;
        }

        public void Insertar(Libro libro)
        {
            NodoLibro nuevo = new NodoLibro(libro);

            if (primero == null)
            {
                primero = nuevo;
                ultimo = nuevo;
            }
            else
            {
                ultimo!.Siguiente = nuevo;
                ultimo = nuevo;
            }

            contador++;
        }

        public Libro? BuscarPorISBN(int isbn)
        {
            NodoLibro? actual = primero;

            while (actual != null)
            {
                if (actual.Libro.ISBN == isbn)
                {
                    return actual.Libro;
                }

                actual = actual.Siguiente;
            }

            return null;
        }

        public bool EliminarPorISBN(int isbn)
        {
            if (primero == null)
            {
                return false;
            }

            if (primero.Libro.ISBN == isbn)
            {
                primero = primero.Siguiente;

                if (primero == null)
                {
                    ultimo = null;
                }

                contador--;
                return true;
            }

            NodoLibro actual = primero;

            while (actual.Siguiente != null)
            {
                if (actual.Siguiente.Libro.ISBN == isbn)
                {
                    if (actual.Siguiente == ultimo)
                    {
                        ultimo = actual;
                    }

                    actual.Siguiente = actual.Siguiente.Siguiente;
                    contador--;

                    return true;
                }

                actual = actual.Siguiente;
            }

            return false;
        }

        public NodoLibro? ObtenerPrimero()
        {
            return primero;
        }
    }
}