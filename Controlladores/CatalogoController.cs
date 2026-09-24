using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.TDA;

namespace GestiónDeBiblioteca.Controlladores
{
    public class CatalogoController
    {
        private ListaLibros listaLibros;
        private ArbolLibrosISBN arbolISBN;

        public CatalogoController()
        {
            listaLibros = new ListaLibros();
            arbolISBN = new ArbolLibrosISBN();
        }

        public bool RegistrarLibro(Libro libro)
        {
            if (arbolISBN.Buscar(libro.ISBN) != null)
            {
                return false;
            }

            listaLibros.Insertar(libro);
            arbolISBN.Insertar(libro);

            return true;
        }

        public bool EliminarLibro(int isbn)
        {
            bool eliminado = arbolISBN.Eliminar(isbn);

            if (eliminado)
            {
                listaLibros.EliminarPorISBN(isbn);
            }

            return eliminado;
        }

        public Libro? BuscarPorISBN(int isbn)
        {
            return arbolISBN.Buscar(isbn);
        }

        public ArbolLibrosISBN ObtenerArbolISBN()
        {
            return arbolISBN;
        }
    }
}