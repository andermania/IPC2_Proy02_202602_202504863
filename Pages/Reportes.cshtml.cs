using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using GestiónDeBiblioteca.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GestiónDeBiblioteca.Pages
{
    /// <summary>
    /// Página de reportes: genera diagramas Graphviz del catálogo.
    /// El renderizado se hace en el navegador con viz.js.
    /// </summary>
    public class ReportesModel : PageModel
    {
        private readonly CatalogoController catalogo;
        private readonly GraphvizService graphvizService;

        [BindProperty]
        public string CategoriaSeleccionada { get; set; } = "";

        public string? DotCategorias { get; set; }
        public string? DotLibrosCategoria { get; set; }

        public SelectList OpcionesCategorias { get; set; } = new SelectList(Array.Empty<SelectListItem>());

        public ReportesModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
            this.graphvizService = new GraphvizService(catalogo);
        }

        public void OnGet()
        {
            CargarComboCategorias();
        }

        public IActionResult OnPostArbolISBN()
        {
            CargarComboCategorias();
            DotCategorias = graphvizService.GenerarDotArbolISBN();
            return Page();
        }

        public IActionResult OnPostEstructuraCategorias()
        {
            CargarComboCategorias();
            DotCategorias = graphvizService.GenerarDotCategorias();
            return Page();
        }

        public IActionResult OnPostLibrosCategoria()
        {
            CargarComboCategorias();

            if (!string.IsNullOrWhiteSpace(CategoriaSeleccionada))
            {
                DotLibrosCategoria = graphvizService.GenerarDotLibrosCategoria(CategoriaSeleccionada);
            }

            return Page();
        }

        /// <summary>
        /// API: retorna DOT del árbol AVL ISBN.
        /// </summary>
        public IActionResult OnGetDotArbolISBN()
        {
            return Content(graphvizService.GenerarDotArbolISBN(), "text/plain");
        }

        /// <summary>
        /// API: retorna DOT de categorías.
        /// </summary>
        public IActionResult OnGetDotCategorias()
        {
            return Content(graphvizService.GenerarDotCategorias(), "text/plain");
        }

        /// <summary>
        /// API: retorna DOT de libros de una categoría.
        /// </summary>
        public IActionResult OnGetDotLibros(string ruta)
        {
            return Content(graphvizService.GenerarDotLibrosCategoria(ruta), "text/plain");
        }

        private void CargarComboCategorias()
        {
            Categoria[] todasLasCategorias = catalogo.ObtenerTodasLasCategorias();

            // Primera pasada: contar categorías con libros (sin List)
            int conLibros = 0;
            for (int i = 0; i < todasLasCategorias.Length; i++)
            {
                if (todasLasCategorias[i].Libros.ObtenerCantidad() > 0)
                {
                    conLibros++;
                }
            }

            SelectListItem[] opciones = new SelectListItem[conLibros];
            int pos = 0;

            for (int i = 0; i < todasLasCategorias.Length; i++)
            {
                if (todasLasCategorias[i].Libros.ObtenerCantidad() > 0)
                {
                    opciones[pos] = new SelectListItem
                    {
                        Value = todasLasCategorias[i].ObtenerRutaCompleta(),
                        Text = todasLasCategorias[i].ObtenerRutaCompleta()
                            + $" ({todasLasCategorias[i].Libros.ObtenerCantidad()} libros)"
                    };
                    pos++;
                }
            }

            OpcionesCategorias = new SelectList(opciones, "Value", "Text");
        }
    }
}
