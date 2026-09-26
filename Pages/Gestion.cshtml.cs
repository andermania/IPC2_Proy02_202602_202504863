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

        // ===================== PROPIEDADES DE VISTA =====================

        public Libro[] Libros { get; set; } = Array.Empty<Libro>();

        public Libro? MenorISBN { get; set; }
        public Libro? MayorISBN { get; set; }

        public Libro? LibroEncontrado { get; set; }
        public string? MensajeBusqueda { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        // Opciones para los combos de categoría/subcategoría
        public SelectList OpcionesCategorias { get; set; } = new SelectList(Array.Empty<SelectListItem>());
        public SelectList OpcionesSubcategorias { get; set; } = new SelectList(Array.Empty<SelectListItem>());

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
            // Determinar la ruta completa de la categoría
            string rutaCategoria = CategoriaSeleccionada;

            if (!string.IsNullOrWhiteSpace(SubcategoriaSeleccionada))
            {
                rutaCategoria = CategoriaSeleccionada + " > " + SubcategoriaSeleccionada;
            }

            Libro libro = new Libro(ISBN, Titulo, Autor, rutaCategoria);

            bool registrado = catalogo.RegistrarLibro(libro);

            if (registrado)
            {
                MensajeExito = $"Libro \"{libro.Titulo}\" registrado exitosamente.";
            }
            else
            {
                MensajeError = $"Ya existe un libro con ISBN {libro.ISBN}.";
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

        public IActionResult OnPostBuscar()
        {
            CargarDatosIniciales();
            CargarCombos();
            Libros = catalogo.ObtenerLibrosAscendente();

            if (BusquedaISBN.HasValue)
            {
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

            return Page();
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
        /// Usa arreglos nativos (sin List) para cumplir restricción de TDAs propios.
        /// </summary>
        private void CargarCombos()
        {
            Categoria[] categoriasRaiz = catalogo.ObtenerCategoriasRaiz();

            SelectListItem[] opcionesCategorias = new SelectListItem[categoriasRaiz.Length];

            for (int i = 0; i < categoriasRaiz.Length; i++)
            {
                opcionesCategorias[i] = new SelectListItem
                {
                    Value = categoriasRaiz[i].Nombre,
                    Text = categoriasRaiz[i].Nombre
                };
            }

            OpcionesCategorias = new SelectList(opcionesCategorias, "Value", "Text");

            // Subcategorías: se cargan desde JavaScript al cambiar el combo de categoría
            // Aquí precargamos si ya hay una categoría seleccionada
            SelectListItem[] opcionesSubcategorias = Array.Empty<SelectListItem>();

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
