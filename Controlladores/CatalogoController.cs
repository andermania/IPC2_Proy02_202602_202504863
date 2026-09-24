using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.TDA;

namespace GestiónDeBiblioteca.Controlladores
{
    /// <summary>
    /// Controlador principal del catálogo de la biblioteca.
    /// Gestiona tres índices AVL (ISBN, Título, Categoría) y un árbol N-ario
    /// de categorías. Mantiene todos los índices sincronizados al agregar
    /// o eliminar libros.
    /// </summary>
    public class CatalogoController
    {
        // Índice principal: AVL por ISBN (búsqueda por código único)
        private ArbolLibrosISBN arbolISBN;

        // Índice secundario: AVL por Título (búsqueda y orden alfabético)
        private ArbolLibrosTitulo arbolTitulo;

        // Árbol jerárquico de categorías (N-ario con profundidad ilimitada)
        private ArbolCategorias arbolCategorias;

        public CatalogoController()
        {
            arbolISBN = new ArbolLibrosISBN();
            arbolTitulo = new ArbolLibrosTitulo();
            arbolCategorias = new ArbolCategorias();
        }

        // ===================== GESTIÓN DE LIBROS =====================

        /// <summary>
        /// Registra un nuevo libro en todos los índices.
        /// Retorna false si el ISBN ya existe.
        /// </summary>
        public bool RegistrarLibro(Libro libro)
        {
            // Verificar que no exista el ISBN
            if (arbolISBN.Buscar(libro.ISBN) != null)
            {
                return false;
            }

            // Insertar en los tres índices
            arbolISBN.Insertar(libro);
            arbolTitulo.Insertar(libro);

            // Insertar en la categoría correspondiente del árbol n-ario
            Categoria? categoria = arbolCategorias.BuscarPorRuta(libro.Categoria);

            if (categoria != null)
            {
                categoria.Libros.Insertar(libro);
            }
            else
            {
                // Si la categoría no existe, crearla como categoría raíz
                Categoria nuevaCat = arbolCategorias.AgregarCategoria(libro.Categoria);
                nuevaCat.Libros.Insertar(libro);
            }

            return true;
        }

        /// <summary>
        /// Elimina un libro de todos los índices.
        /// Retorna false si no se encontró.
        /// </summary>
        public bool EliminarLibro(int isbn)
        {
            // Buscar el libro primero para saber su categoría
            Libro? libro = arbolISBN.Buscar(isbn);

            if (libro == null)
            {
                return false;
            }

            // Eliminar de los tres índices
            bool eliminadoISBN = arbolISBN.Eliminar(isbn);
            arbolTitulo.Eliminar(isbn);

            // Eliminar del árbol de categorías
            Categoria? categoria = arbolCategorias.BuscarPorRuta(libro.Categoria);

            if (categoria != null)
            {
                categoria.Libros.Eliminar(isbn);
            }

            return eliminadoISBN;
        }

        /// <summary>
        /// Busca un libro por ISBN.
        /// </summary>
        public Libro? BuscarPorISBN(int isbn)
        {
            return arbolISBN.Buscar(isbn);
        }

        /// <summary>
        /// Busca un libro por título.
        /// </summary>
        public Libro? BuscarPorTitulo(string titulo)
        {
            return arbolTitulo.BuscarPorTitulo(titulo);
        }

        /// <summary>
        /// Retorna el libro con el ISBN más bajo.
        /// </summary>
        public Libro? ObtenerMenorISBN()
        {
            return arbolISBN.ObtenerMenorISBN();
        }

        /// <summary>
        /// Retorna el libro con el ISBN más alto.
        /// </summary>
        public Libro? ObtenerMayorISBN()
        {
            return arbolISBN.ObtenerMayorISBN();
        }

        /// <summary>
        /// Retorna todos los libros ordenados ascendente por ISBN.
        /// </summary>
        public Libro[] ObtenerLibrosAscendente()
        {
            return arbolISBN.ObtenerAscendente();
        }

        /// <summary>
        /// Retorna todos los libros ordenados descendente por ISBN.
        /// </summary>
        public Libro[] ObtenerLibrosDescendente()
        {
            return arbolISBN.ObtenerDescendente();
        }

        /// <summary>
        /// Retorna todos los libros ordenados alfabéticamente por título.
        /// </summary>
        public Libro[] ObtenerLibrosAlfabetico()
        {
            return arbolTitulo.ObtenerOrdenAlfabetico();
        }

        /// <summary>
        /// Retorna el total de libros en el catálogo.
        /// </summary>
        public int ObtenerTotalLibros()
        {
            return arbolISBN.ObtenerCantidad();
        }

        // ===================== GESTIÓN DE CATEGORÍAS =====================

        /// <summary>
        /// Agrega una categoría raíz al árbol.
        /// </summary>
        public Categoria AgregarCategoria(string nombre)
        {
            return arbolCategorias.AgregarCategoria(nombre);
        }

        /// <summary>
        /// Agrega una subcategoría bajo una ruta de padre.
        /// Crea las categorías intermedias si no existen.
        /// </summary>
        public Categoria AgregarSubcategoria(string rutaPadre, string nombreHija)
        {
            return arbolCategorias.AgregarSubcategoria(rutaPadre, nombreHija);
        }

        /// <summary>
        /// Retorna todas las categorías raíz.
        /// </summary>
        public Categoria[] ObtenerCategoriasRaiz()
        {
            return arbolCategorias.ObtenerCategoriasRaiz();
        }

        /// <summary>
        /// Retorna todas las categorías del árbol (preorden).
        /// </summary>
        public Categoria[] ObtenerTodasLasCategorias()
        {
            return arbolCategorias.ObtenerTodasLasCategorias();
        }

        /// <summary>
        /// Busca una categoría por ruta completa.
        /// </summary>
        public Categoria? BuscarCategoria(string ruta)
        {
            return arbolCategorias.BuscarPorRuta(ruta);
        }

        /// <summary>
        /// Retorna los libros que pertenecen a una categoría específica.
        /// </summary>
        public Libro[] ObtenerLibrosPorCategoria(string rutaCategoria)
        {
            Categoria? categoria = arbolCategorias.BuscarPorRuta(rutaCategoria);

            if (categoria == null)
            {
                return new Libro[0];
            }

            return categoria.Libros.ObtenerAscendente();
        }

        /// <summary>
        /// Retorna la estructura completa de categorías en texto.
        /// </summary>
        public string ObtenerEstructuraCategorias()
        {
            return arbolCategorias.GenerarEstructuraTexto();
        }

        /// <summary>
        /// Retorna la estructura de categorías desde una ruta específica.
        /// </summary>
        public string ObtenerEstructuraDesde(string ruta)
        {
            return arbolCategorias.GenerarEstructuraDesde(ruta);
        }

        /// <summary>
        /// Retorna el total de categorías.
        /// </summary>
        public int ObtenerTotalCategorias()
        {
            return arbolCategorias.ObtenerTotalCategorias();
        }

        // ===================== ACCESO A INDICES =====================

        public ArbolLibrosISBN ObtenerArbolISBN()
        {
            return arbolISBN;
        }

        public ArbolLibrosTitulo ObtenerArbolTitulo()
        {
            return arbolTitulo;
        }

        public ArbolCategorias ObtenerArbolCategorias()
        {
            return arbolCategorias;
        }
    }
}
