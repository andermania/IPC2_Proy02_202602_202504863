using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestiónDeBiblioteca.Pages
{
    /// <summary>
    /// Página de catálogo: muestra la estructura jerárquica de categorías
    /// y permite agregar categorías/subcategorías nuevas.
    /// </summary>
    public class CatalogoModel : PageModel
    {
        private readonly CatalogoController catalogo;
        private readonly GraphvizService graphvizService;

        [BindProperty]
        public string NuevaCategoria { get; set; } = "";

        [BindProperty]
        public string CategoriaPadre { get; set; } = "";

        [BindProperty]
        public string NuevaSubcategoria { get; set; } = "";

        [BindProperty]
        public string RutaVisualizar { get; set; } = "";

        public string EstructuraTexto { get; set; } = "";
        public string? EstructuraParcial { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }
        public int TotalCategorías { get; set; }
        public int TotalLibros { get; set; }

        public SelectList OpcionesCategorias { get; set; } = new SelectList(Array.Empty<SelectListItem>());
        public SelectList OpcionesTodasCategorias { get; set; } = new SelectList(Array.Empty<SelectListItem>());

        public CatalogoModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
            this.graphvizService = new GraphvizService(catalogo);
        }

        public void OnGet()
        {
            CargarDatos();
        }

        public IActionResult OnPostAgregarCategoria()
        {
            if (string.IsNullOrWhiteSpace(NuevaCategoria))
            {
                MensajeError = "Debe ingresar un nombre para la categoría.";
                CargarDatos();
                return Page();
            }

            Categoria? existente = catalogo.BuscarCategoria(NuevaCategoria.Trim());

            if (existente != null)
            {
                MensajeError = $"La categoría \"{NuevaCategoria.Trim()}\" ya existe.";
                CargarDatos();
                return Page();
            }

            catalogo.AgregarCategoria(NuevaCategoria.Trim());
            MensajeExito = $"Categoría \"{NuevaCategoria.Trim()}\" creada exitosamente.";
            CargarDatos();
            return Page();
        }

        public IActionResult OnPostAgregarSubcategoria()
        {
            if (string.IsNullOrWhiteSpace(CategoriaPadre) || string.IsNullOrWhiteSpace(NuevaSubcategoria))
            {
                MensajeError = "Debe seleccionar un padre e ingresar el nombre de la subcategoría.";
                CargarDatos();
                return Page();
            }

            string rutaCompleta = CategoriaPadre.Trim() + " > " + NuevaSubcategoria.Trim();
            Categoria? existente = catalogo.BuscarCategoria(rutaCompleta);

            if (existente != null)
            {
                MensajeError = $"La subcategoría \"{rutaCompleta}\" ya existe.";
                CargarDatos();
                return Page();
            }

            catalogo.AgregarSubcategoria(CategoriaPadre.Trim(), NuevaSubcategoria.Trim());
            MensajeExito = $"Subcategoría \"{NuevaSubcategoria.Trim()}\" creada bajo \"{CategoriaPadre.Trim()}\".";
            CargarDatos();
            return Page();
        }

        public IActionResult OnPostVisualizarDesde()
        {
            CargarDatos();

            if (string.IsNullOrWhiteSpace(RutaVisualizar))
            {
                EstructuraParcial = null;
            }
            else
            {
                EstructuraParcial = catalogo.ObtenerEstructuraDesde(RutaVisualizar);
            }

            return Page();
        }

        /// <summary>
        /// API endpoint que retorna el DOT de categorías para renderizar en el navegador.
        /// </summary>
        public IActionResult OnGetDotCategorias()
        {
            string dot = graphvizService.GenerarDotCategorias();
            return Content(dot, "text/plain");
        }

        private void CargarDatos()
        {
            EstructuraTexto = catalogo.ObtenerEstructuraCategorias();
            TotalCategorías = catalogo.ObtenerTotalCategorias();
            TotalLibros = catalogo.ObtenerTotalLibros();

            Categoria[] todasLasCategorias = catalogo.ObtenerTodasLasCategorias();

            List<SelectListItem> opcionesCategorias = new List<SelectListItem>();
            List<SelectListItem> opcionesTodas = new List<SelectListItem>();

            for (int i = 0; i < todasLasCategorias.Length; i++)
            {
                opcionesCategorias.Add(new SelectListItem
                {
                    Value = todasLasCategorias[i].Nombre,
                    Text = todasLasCategorias[i].ObtenerRutaCompleta()
                });

                opcionesTodas.Add(new SelectListItem
                {
                    Value = todasLasCategorias[i].ObtenerRutaCompleta(),
                    Text = todasLasCategorias[i].ObtenerRutaCompleta()
                        + $" ({todasLasCategorias[i].Libros.ObtenerCantidad()} libros)"
                });
            }

            OpcionesCategorias = new SelectList(opcionesCategorias, "Value", "Text");
            OpcionesTodasCategorias = new SelectList(opcionesTodas, "Value", "Text");
        }
    }
}
