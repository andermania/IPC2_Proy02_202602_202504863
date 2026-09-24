using GestiónDeBiblioteca.Controlladores;
using GestiónDeBiblioteca.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GestiónDeBiblioteca.Pages
{
    public class GestionModel : PageModel
    {
        private readonly CatalogoController catalogo;

        [BindProperty]
        public int ISBN { get; set; }

        [BindProperty]
        public string Titulo { get; set; } = "";

        [BindProperty]
        public string Autor { get; set; } = "";

        [BindProperty]
        public string Categoria { get; set; } = "";

        [BindProperty]
        public string LibrosSeleccionados { get; set; } = "";

        public Libro[] Libros { get; set; } = Array.Empty<Libro>();

        public GestionModel(CatalogoController catalogo)
        {
            this.catalogo = catalogo;
        }

        public void OnGet(string? orden)
        {
            if (orden == "desc")
            {
                Libros = catalogo
                    .ObtenerArbolISBN()
                    .ObtenerDescendente();
            }
            else
            {
                Libros = catalogo
                    .ObtenerArbolISBN()
                    .ObtenerAscendente();
            }
        }

        public IActionResult OnPostRegistrar()
        {
            Libro libro = new Libro(
                ISBN,
                Titulo,
                Autor,
                Categoria
            );

            catalogo.RegistrarLibro(libro);

            return RedirectToPage();
        }

        public IActionResult OnPostEliminar()
        {
            if (!string.IsNullOrWhiteSpace(LibrosSeleccionados))
            {
                string[] isbns =
                    LibrosSeleccionados.Split(',');

                foreach (string isbn in isbns)
                {
                    if (int.TryParse(isbn, out int numeroISBN))
                    {
                        catalogo.EliminarLibro(numeroISBN);
                    }
                }
            }

            return RedirectToPage();
        }
    }
}