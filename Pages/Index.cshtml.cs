using GestiónDeBiblioteca.Controlladores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestiónDeBiblioteca.Pages
{
    public class IndexModel : PageModel
    {
        private readonly CatalogoController catalogo;

        public string? MensajeExito { get; set; }
        public int TotalLibros { get; set; }
        public int TotalCategorias { get; set; }

        public IndexModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
        }

        public void OnGet()
        {
            TotalLibros = catalogo.ObtenerTotalLibros();
            TotalCategorias = catalogo.ObtenerTotalCategorias();
        }

        /// <summary>
        /// Inicialización: deja el sistema sin ninguna información previa.
        /// </summary>
        public IActionResult OnPostReiniciar()
        {
            catalogo.Reiniciar();
            MensajeExito = "Sistema inicializado: catálogo vacío listo para cargar datos.";
            TotalLibros = 0;
            TotalCategorias = catalogo.ObtenerTotalCategorias();
            return Page();
        }
    }
}
