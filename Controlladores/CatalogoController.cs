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
        /// Retorna false si el ISBN ya existe o si la categoría no existe.
        /// La categoría se resuelve por ruta y, si falla, por nombre global
        /// (los libros XML referencian solo el nombre hoja).
        /// NO crea categorías automáticamente: el XML debe declararlas antes.
        /// </summary>
        public bool RegistrarLibro(Libro libro)
        {
            // Verificar que no exista el ISBN
            if (arbolISBN.Buscar(libro.ISBN) != null)
            {
                return false;
            }

            // Resolver categoría (ruta completa o nombre hoja global)
            Categoria? categoria = arbolCategorias.BuscarPorRuta(libro.Categoria);

            if (categoria == null)
            {
                categoria = arbolCategorias.BuscarPorNombreGlobal(libro.Categoria.Trim());
            }

            if (categoria == null)
            {
                return false;
            }

            // Insertar en los tres índices
            arbolISBN.Insertar(libro);
            arbolTitulo.Insertar(libro);
            categoria.Libros.Insertar(libro);

            return true;
        }

        /// <summary>
        /// Elimina un libro de todos los índices.
        /// Retorna false si no se encontró.
        /// </summary>
        public bool EliminarLibro(int isbn)
        {
            // Buscar el libro primero para saber su categoría y título
            Libro? libro = arbolISBN.Buscar(isbn);

            if (libro == null)
            {
                return false;
            }

            // Eliminar de los tres índices (el AVL de títulos se indexa por título+ISBN)
            bool eliminadoISBN = arbolISBN.Eliminar(isbn);
            arbolTitulo.Eliminar(libro.Titulo, libro.ISBN);

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

        // ===================== TRASLADO ENTRE CATEGORÍAS =====================

        /// <summary>
        /// Traslada todos los libros de la categoría de origen a la categoría de destino.
        /// Se recorre el árbol AVL de libros de la categoría de origen en preorden y
        /// cada libro se inserta en el árbol AVL de la categoría de destino con la
        /// operación de inserción propia (que mantiene el balance del AVL destino).
        /// Los libros conservan su ISBN, título y autor; solo cambian de categoría.
        /// Al finalizar, la categoría de origen queda sin los libros trasladados.
        /// Retorna null si el traslado fue correcto; si no, un mensaje de error.
        /// </summary>
        public string? MoverLibrosEntreCategorias(string rutaOrigen, string rutaDestino, out int totalTrasladados)
        {
            totalTrasladados = 0;

            if (string.IsNullOrWhiteSpace(rutaOrigen) || string.IsNullOrWhiteSpace(rutaDestino))
            {
                return "Debe seleccionar una categoría de origen y una de destino.";
            }

            Categoria? origen = arbolCategorias.BuscarPorRuta(rutaOrigen.Trim());

            if (origen == null)
            {
                origen = arbolCategorias.BuscarPorNombreGlobal(rutaOrigen.Trim());
            }

            Categoria? destino = arbolCategorias.BuscarPorRuta(rutaDestino.Trim());

            if (destino == null)
            {
                destino = arbolCategorias.BuscarPorNombreGlobal(rutaDestino.Trim());
            }

            if (origen == null)
            {
                return $"La categoría de origen \"{rutaOrigen.Trim()}\" no existe.";
            }

            if (destino == null)
            {
                return $"La categoría de destino \"{rutaDestino.Trim()}\" no existe.";
            }

            if (ReferenceEquals(origen, destino))
            {
                return "La categoría de origen y la de destino deben ser diferentes.";
            }

            if (origen.Libros.ObtenerCantidad() == 0)
            {
                return "La categoría de origen no contiene libros disponibles para trasladar.";
            }

            // Capturar los libros de origen (referencias reales, sin duplicar datos).
            Libro[] librosOrigen = origen.Libros.ObtenerAscendente();
            totalTrasladados = librosOrigen.Length;

            // Recorrido preorden del AVL de origen: cada libro se inserta en el AVL
            // de destino usando la operación propia (mantiene el balance).
            origen.Libros.TrasladarPreorden(destino.Libros);

            // Los libros ahora pertenecen a la categoría destino: se actualiza únicamente
            // la referencia de ubicación (no se tocan ISBN, título ni autor).
            string rutaDestinoFinal = destino.ObtenerRutaCompleta();

            for (int i = 0; i < librosOrigen.Length; i++)
            {
                librosOrigen[i].Categoria = rutaDestinoFinal;
            }

            return null;
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
        /// Retorna null si el padre no existe (no crea fantasmas).
        /// </summary>
        public Categoria? AgregarSubcategoria(string rutaPadre, string nombreHija)
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
        /// Busca una categoría por nombre en todo el árbol (unicidad global).
        /// </summary>
        public Categoria? BuscarCategoriaPorNombre(string nombre)
        {
            return arbolCategorias.BuscarPorNombreGlobal(nombre);
        }

        /// <summary>
        /// Reinicia todo el catálogo (inicialización sin información previa).
        /// Limpia los tres índices y el árbol de categorías.
        /// </summary>
        public void Reiniciar()
        {
            arbolISBN = new ArbolLibrosISBN();
            arbolTitulo = new ArbolLibrosTitulo();
            arbolCategorias = new ArbolCategorias();
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
