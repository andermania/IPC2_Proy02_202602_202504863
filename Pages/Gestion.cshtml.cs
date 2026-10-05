using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestiónDeBiblioteca.Pages
{
    /// <summary>
    /// Página de gestión de libros: registro, búsqueda, eliminación y consulta.
    /// Alimenta el combo de categorías desde el árbol N-ario.
    /// </summary>
    public class GestionModel : PageModel
    {
        private readonly CatalogoController catalogo;

        // ===================== PROPIEDADES DE BIND =====================

        [BindProperty]
        public int ISBN { get; set; }

        [BindProperty]
        public string Titulo { get; set; } = "";

        [BindProperty]
        public string Autor { get; set; } = "";

        [BindProperty]
        public string CategoriaSeleccionada { get; set; } = "";

        [BindProperty]
        public string SubcategoriaSeleccionada { get; set; } = "";

        [BindProperty]
        public string LibrosSeleccionados { get; set; } = "";

        [BindProperty]
        public int? BusquedaISBN { get; set; }

        [BindProperty]
        public string? BusquedaTitulo { get; set; }

        // ===================== PROPIEDADES DE VISTA =====================

        public Libro[] Libros { get; set; } = new Libro[0];

        public Libro? MenorISBN { get; set; }
        public Libro? MayorISBN { get; set; }

        public Libro? LibroEncontrado { get; set; }
        public string? MensajeBusqueda { get; set; }

        public Libro? LibroEncontradoTitulo { get; set; }
        public string? MensajeBusquedaTitulo { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        // Opciones para los combos de categoría/subcategoría
        public SelectList OpcionesCategorias { get; set; } = new SelectList(new SelectListItem[0]);
        public SelectList OpcionesSubcategorias { get; set; } = new SelectList(new SelectListItem[0]);

        // ===================== CONSTRUCTOR =====================

        public GestionModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
        }

        // ===================== HANDLERS =====================

        public void OnGet(string? orden)
        {
            CargarDatosIniciales();
            CargarCombos();

            if (orden == "desc")
            {
                Libros = catalogo.ObtenerLibrosDescendente();
            }
            else
            {
                Libros = catalogo.ObtenerLibrosAscendente();
            }
        }

        public IActionResult OnPostRegistrar()
        {
            // CategoriaSeleccionada ya trae ruta completa (ej: "Catalogo > Ecologia > Estadistica").
            // SubcategoriaSeleccionada es el nivel siguiente (para jerarquía profunda).
            string rutaCategoria = (CategoriaSeleccionada ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(SubcategoriaSeleccionada))
            {
                string sub = SubcategoriaSeleccionada.Trim();
                rutaCategoria = string.IsNullOrEmpty(rutaCategoria)
                    ? sub
                    : rutaCategoria + " > " + sub;
            }

            if (string.IsNullOrWhiteSpace(rutaCategoria))
            {
                MensajeError = "Debe seleccionar una categoría.";
                CargarDatosIniciales();
                CargarCombos();
                Libros = catalogo.ObtenerLibrosAscendente();
                return Page();
            }

            // Validar que la categoría exista (cargada desde XML)
            Categoria? catExiste = catalogo.BuscarCategoria(rutaCategoria);

            if (catExiste == null)
            {
                catExiste = catalogo.BuscarCategoriaPorNombre(rutaCategoria);
            }

            if (catExiste == null)
            {
                MensajeError = $"La categoría \"{rutaCategoria}\" no existe. Cargue primero el XML.";
                CargarDatosIniciales();
                CargarCombos();
                Libros = catalogo.ObtenerLibrosAscendente();
                return Page();
            }

            string rutaReal = catExiste.ObtenerRutaCompleta();
            Libro libro = new Libro(ISBN, Titulo, Autor, rutaReal);

            bool registrado = catalogo.RegistrarLibro(libro);

            if (registrado)
            {
                MensajeExito = $"Libro \"{libro.Titulo}\" registrado exitosamente en \"{rutaReal}\".";
            }
            else
            {
                MensajeError = catalogo.BuscarPorISBN(libro.ISBN) != null
                    ? $"Ya existe un libro con ISBN {libro.ISBN}."
                    : $"No se pudo registrar el libro (categoría inválida).";
            }

            CargarDatosIniciales();
            CargarCombos();

            // Mantener el orden actual
            Libros = catalogo.ObtenerLibrosAscendente();
            return Page();
        }

        public IActionResult OnPostEliminar()
        {
            if (!string.IsNullOrWhiteSpace(LibrosSeleccionados))
            {
                string[] isbns = LibrosSeleccionados.Split(',');
                int eliminados = 0;

                foreach (string isbn in isbns)
                {
                    if (int.TryParse(isbn, out int numeroISBN))
                    {
                        if (catalogo.EliminarLibro(numeroISBN))
                        {
                            eliminados++;
                        }
                    }
                }

                MensajeExito = $"Se eliminaron {eliminados} libro(s).";
            }

            CargarDatosIniciales();
            CargarCombos();
            Libros = catalogo.ObtenerLibrosAscendente();
            return Page();
        }

        /// <summary>
        /// Elimina un único libro (ej: desde el basurero de la tarjeta de búsqueda).
        /// CatalogoController.EliminarLibro lo quita de los tres índices
        /// (AVL ISBN, AVL Título y AVL de su categoría); cada AVL se
        /// rebalancea con rotaciones al subir de la recursión.
        /// </summary>
        public IActionResult OnPostEliminarIndividual(int isbn)
        {
            bool eliminado = catalogo.EliminarLibro(isbn);

            if (eliminado)
            {
                MensajeExito = $"Libro con ISBN {isbn} eliminado de todo el catálogo.";
            }
            else
            {
                MensajeError = $"No se encontró ningún libro con ISBN {isbn}.";
            }

            CargarDatosIniciales();
            CargarCombos();
            Libros = catalogo.ObtenerLibrosAscendente();
            return Page();
        }

        public IActionResult OnPostBuscar()
        {
            CargarDatosIniciales();
            CargarCombos();
            Libros = catalogo.ObtenerLibrosAscendente();

            bool buscoAlgo = false;

            // Búsqueda por ISBN sobre el AVL indexado por ISBN — O(log n)
            if (BusquedaISBN.HasValue)
            {
                buscoAlgo = true;
                Libro? encontrado = catalogo.BuscarPorISBN(BusquedaISBN.Value);

                if (encontrado != null)
                {
                    LibroEncontrado = encontrado;
                }
                else
                {
                    MensajeBusqueda = $"No se encontró ningún libro con ISBN {BusquedaISBN.Value}.";
                }
            }

            // Búsqueda por Título sobre el AVL alfabético — O(log n).
            // Ignora mayúsculas/minúsculas; si hay varios libros con el mismo
            // título, retorna el primero encontrado.
            if (!string.IsNullOrWhiteSpace(BusquedaTitulo))
            {
                buscoAlgo = true;
                string titulo = BusquedaTitulo.Trim();
                Libro? encontrado = catalogo.BuscarPorTitulo(titulo);

                if (encontrado != null)
                {
                    LibroEncontradoTitulo = encontrado;
                }
                else
                {
                    MensajeBusquedaTitulo = $"No se encontró ningún libro con título \"{titulo}\".";
                }
            }

            if (!buscoAlgo)
            {
                MensajeBusqueda = "Ingrese un ISBN o un título para buscar.";
            }

            return Page();
        }

        /// <summary>
        /// API JSON: retorna las subcategorías hijas directas de una categoría.
        /// Usado por JavaScript para cargar el combo de subcategoría al cambiar la categoría.
        /// </summary>
        public IActionResult OnGetSubcategorias(string categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria))
            {
                return new JsonResult(new object[0]);
            }

            Categoria? cat = catalogo.BuscarCategoria(categoria);

            if (cat == null)
            {
                cat = catalogo.BuscarCategoriaPorNombre(categoria);
            }

            if (cat == null)
            {
                return new JsonResult(new object[0]);
            }

            Categoria[] hijos = cat.Hijos.ObtenerTodas();
            object[] resultado = new object[hijos.Length];

            for (int i = 0; i < hijos.Length; i++)
            {
                resultado[i] = new { value = hijos[i].Nombre, text = hijos[i].Nombre };
            }

            return new JsonResult(resultado);
        }

        // ===================== MÉTODOS AUXILIARES =====================

        /// <summary>
        /// Carga los datos iniciales de la página (min/max ISBN).
        /// </summary>
        private void CargarDatosIniciales()
        {
            MenorISBN = catalogo.ObtenerMenorISBN();
            MayorISBN = catalogo.ObtenerMayorISBN();
        }

        /// <summary>
        /// Carga los combos de categorías y subcategorías desde el árbol N-ario.
        /// El combo principal lista TODAS las categorías con ruta completa
        /// (soporta profundidad ilimitada: Catalogo > Ecologia > Estadistica > ...).
        /// El segundo combo carga los hijos directos de la selección vía JS.
        /// Usa arreglos nativos (sin List) para cumplir restricción de TDAs propios.
        /// </summary>
        private void CargarCombos()
        {
            Categoria[] todas = catalogo.ObtenerTodasLasCategorias();

            SelectListItem[] opcionesCategorias = new SelectListItem[todas.Length];

            for (int i = 0; i < todas.Length; i++)
            {
                string ruta = todas[i].ObtenerRutaCompleta();
                opcionesCategorias[i] = new SelectListItem
                {
                    Value = ruta,
                    Text = ruta
                };
            }

            OpcionesCategorias = new SelectList(opcionesCategorias, "Value", "Text");

            // Subcategorías: se cargan desde JavaScript al cambiar el combo de categoría
            // Aquí precargamos si ya hay una categoría seleccionada
            SelectListItem[] opcionesSubcategorias = new SelectListItem[0];

            if (!string.IsNullOrWhiteSpace(CategoriaSeleccionada))
            {
                Categoria? categoria = catalogo.BuscarCategoria(CategoriaSeleccionada);

                if (categoria == null)
                {
                    categoria = catalogo.BuscarCategoriaPorNombre(CategoriaSeleccionada);
                }

                if (categoria != null)
                {
                    Categoria[] subcategorias = categoria.Hijos.ObtenerTodas();
                    opcionesSubcategorias = new SelectListItem[subcategorias.Length];

                    for (int i = 0; i < subcategorias.Length; i++)
                    {
                        opcionesSubcategorias[i] = new SelectListItem
                        {
                            Value = subcategorias[i].Nombre,
                            Text = subcategorias[i].Nombre
                        };
                    }
                }
            }

            OpcionesSubcategorias = new SelectList(opcionesSubcategorias, "Value", "Text");
        }
    }
}
