using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Nodo de la lista enlazada de categorías hijas.
    /// Se usa para mantener las subcategorías de una categoría padre.
    /// </summary>
    public class NodoCategoriaLista
    {
        public Categoria Categoria { get; set; }
        public NodoCategoriaLista? Siguiente { get; set; }

        public NodoCategoriaLista(Categoria categoria)
        {
            Categoria = categoria;
            Siguiente = null;
        }
    }

    /// <summary>
    /// Lista enlazada simple para almacenar categorías hijas.
    /// Mantiene las subcategorías ordenadas alfabéticamente al insertar.
    /// No utiliza estructuras de C# (List, etc.).
    /// </summary>
    public class ListaCategorias
    {
        private NodoCategoriaLista? cabeza;
        private int contador;

        public ListaCategorias()
        {
            cabeza = null;
            contador = 0;
        }

        /// <summary>
        /// Inserta una categoría manteniendo orden alfabético.
        /// </summary>
        public void InsertarOrdenado(Categoria categoria)
        {
            NodoCategoriaLista nuevo = new NodoCategoriaLista(categoria);

            // Si la lista está vacía o la nueva va antes de la cabeza
            if (cabeza == null
                || string.Compare(categoria.Nombre, cabeza.Categoria.Nombre,
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                nuevo.Siguiente = cabeza;
                cabeza = nuevo;
            }
            else
            {
                // Buscar la posición correcta
                NodoCategoriaLista actual = cabeza;

                while (actual.Siguiente != null
                    && string.Compare(categoria.Nombre, actual.Siguiente.Categoria.Nombre,
                        StringComparison.OrdinalIgnoreCase) > 0)
                {
                    actual = actual.Siguiente;
                }

                nuevo.Siguiente = actual.Siguiente;
                actual.Siguiente = nuevo;
            }

            contador++;
        }

        /// <summary>
        /// Busca una categoría por nombre (ignorando mayúsculas).
        /// </summary>
        public Categoria? BuscarPorNombre(string nombre)
        {
            NodoCategoriaLista? actual = cabeza;

            while (actual != null)
            {
                if (string.Equals(actual.Categoria.Nombre, nombre,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return actual.Categoria;
                }

                actual = actual.Siguiente;
            }

            return null;
        }

        /// <summary>
        /// Elimina una categoría por nombre.
        /// </summary>
        public bool EliminarPorNombre(string nombre)
        {
            if (cabeza == null)
            {
                return false;
            }

            if (string.Equals(cabeza.Categoria.Nombre, nombre,
                StringComparison.OrdinalIgnoreCase))
            {
                cabeza = cabeza.Siguiente;
                contador--;
                return true;
            }

            NodoCategoriaLista actual = cabeza;

            while (actual.Siguiente != null)
            {
                if (string.Equals(actual.Siguiente.Categoria.Nombre, nombre,
                    StringComparison.OrdinalIgnoreCase))
                {
                    actual.Siguiente = actual.Siguiente.Siguiente;
                    contador--;
                    return true;
                }

                actual = actual.Siguiente;
            }

            return false;
        }

        /// <summary>
        /// Retorna un array con todas las categorías hijas (para iterar).
        /// </summary>
        public Categoria[] ObtenerTodas()
        {
            Categoria[] categorias = new Categoria[contador];
            NodoCategoriaLista? actual = cabeza;
            int i = 0;

            while (actual != null)
            {
                categorias[i] = actual.Categoria;
                i++;
                actual = actual.Siguiente;
            }

            return categorias;
        }

        public int ObtenerCantidad()
        {
            return contador;
        }

        public bool EstaVacia()
        {
            return cabeza == null;
        }
    }
}
