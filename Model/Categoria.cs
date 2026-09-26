using GestiónDeBiblioteca.TDA;

namespace GestiónDeBiblioteca.Model
{
    /// <summary>
    /// Representa una categoría o subcategoría del catálogo de la biblioteca.
    /// Cada categoría puede tener un padre (nulo si es raíz), subcategorías hijas
    /// y un índice AVL de libros que pertenecen a ella.
    /// Las categorías se identifican por su nombre (único a nivel de hermanos).
    /// </summary>
    public class Categoria
    {
        /// <summary>
        /// Nombre único de la categoría (ej: "Ficción", "Ciencia", "Tecnología").
        /// No puede haber dos categorías hermanas con el mismo nombre.
        /// </summary>
        public string Nombre { get; set; }

        /// <summary>
        /// Referencia a la categoría padre. Null si es una categoría raíz.
        /// </summary>
        public Categoria? Padre { get; set; }

        /// <summary>
        /// Lista enlazada de subcategorías hijas, ordenadas alfabéticamente.
        /// Puede estar vacía si es una categoría hoja.
        /// </summary>
        public ListaCategorias Hijos { get; set; }

        /// <summary>
        /// Árbol AVL de libros que pertenecen directamente a esta categoría.
        /// Indexado por ISBN para búsquedas rápidas.
        /// </summary>
        public ArbolLibrosISBN Libros { get; set; }

        public Categoria(string nombre)
        {
            Nombre = nombre;
            Padre = null;
            Hijos = new ListaCategorias();
            Libros = new ArbolLibrosISBN();
        }

        public Categoria(string nombre, Categoria padre) : this(nombre)
        {
            Padre = padre;
        }

        /// <summary>
        /// Retorna la ruta completa de la categoría (ej: "Ciencia > Física").
        /// Excluye la raíz virtual "Biblioteca" para rutas limpias en la UI.
        /// </summary>
        public string ObtenerRutaCompleta()
        {
            if (Padre == null)
            {
                return Nombre;
            }

            // No incluir la raíz virtual en la ruta visible
            if (Padre.Padre == null && Padre.Nombre == "Biblioteca")
            {
                return Nombre;
            }

            return Padre.ObtenerRutaCompleta() + " > " + Nombre;
        }

        /// <summary>
        /// Retorna true si es una categoría raíz (sin padre).
        /// </summary>
        public bool EsRaiz()
        {
            return Padre == null;
        }

        /// <summary>
        /// Retorna true si es una categoría hoja (sin hijos).
        /// </summary>
        public bool EsHoja()
        {
            return Hijos.EstaVacia();
        }

        /// <summary>
        /// Cuenta el total de libros en esta categoría y todas sus subcategorías.
        /// </summary>
        public int ContarLibrosTotales()
        {
            int total = Libros.ObtenerCantidad();

            Categoria[] hijos = Hijos.ObtenerTodas();
            for (int i = 0; i < hijos.Length; i++)
            {
                total += hijos[i].ContarLibrosTotales();
            }

            return total;
        }

        public override string ToString()
        {
            return $"{Nombre} ({Libros.ObtenerCantidad()} libros, {Hijos.ObtenerCantidad()} subcat.)";
        }
    }
}
