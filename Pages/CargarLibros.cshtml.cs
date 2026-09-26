using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestiónDeBiblioteca.Pages
{
    /// <summary>
    /// Página para cargar datos desde archivos XML de entrada.
    /// Permite subir un archivo XML que contiene categorías y libros.
    /// </summary>
    public class CargarLibrosModel : PageModel
    {
        private readonly CatalogoController catalogo;
        private readonly XmlService xmlService;

        public XmlService.ResultadoCarga? Resultado { get; set; }
        public string? MensajeExito { get; set; }
        public string? MensajeError { get; set; }

        public CargarLibrosModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
            this.xmlService = new XmlService(catalogo);
        }

        public void OnGet()
        {
        }

        /// <summary>
        /// Descarga un XML de ejemplo con el formato esperado.
        /// </summary>
        public IActionResult OnGetXmlEjemplo()
        {
            string xml = XmlService.GenerarXmlEjemplo();
            return Content(xml, "application/xml", System.Text.Encoding.UTF8);
        }

        public IActionResult OnPostCargar(IFormFile? archivoXml)
        {
            if (archivoXml == null || archivoXml.Length == 0)
            {
                MensajeError = "No se seleccionó ningún archivo.";
                return Page();
            }

            if (!archivoXml.FileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                MensajeError = "El archivo debe tener extensión .xml";
                return Page();
            }

            try
            {
                using (Stream stream = archivoXml.OpenReadStream())
                {
                    Resultado = xmlService.ProcesarXml(stream);
                }

                if (Resultado.LibrosCargados > 0 || Resultado.CategoriasCreadas > 0)
                {
                    MensajeExito = $"Carga completada: {Resultado.LibrosCargados} libros cargados, "
                        + $"{Resultado.CategoriasCreadas} categorías creadas.";
                }
                else if (Resultado.Errores.ObtenerCantidad() > 0)
                {
                    MensajeError = "No se cargaron datos. Revise los errores.";
                }
                else
                {
                    MensajeError = "El archivo XML no contenía datos válidos.";
                }
            }
            catch (Exception ex)
            {
                MensajeError = "Error al procesar el archivo: " + ex.Message;
            }

            return Page();
        }
    }
}
