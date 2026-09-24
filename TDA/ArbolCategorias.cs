using GestiónDeBiblioteca.Model;

namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Árbol N-ario que representa la jerarquía de categorías de la biblioteca.
    /// Soporta profundidad ilimitada (categorías → subcategorías → sub-subcategorías...).
    /// Cada nodo categoría contiene un AVL de libros asociados.
    /// Las subcategorías se mantienen ordenadas alfabéticamente.
    /// </summary>
    public class ArbolCategorias
    {
        private Categoria raiz;
        private int totalCategorias;

        public ArbolCategorias()
        {
            // La raíz es una categoría virtual que agrupa todo
            raiz = new Categoria("Biblioteca");
            totalCategorias = 1;
        }

        /// <summary>
        /// Retorna la categoría raíz del árbol.
        /// </summary>
        public Categoria ObtenerRaiz()
        {
            return raiz;
        }

        /// <summary>
        /// Agrega una categoría como hija directa de la raíz.
        /// Si ya existe, retorna la existente.
        /// </summary>
        public Categoria AgregarCategoria(string nombre)
        {
            Categoria? existente = raiz.Hijos.BuscarPorNombre(nombre);

            if (existente != null)
            {
                return existente;
            }

            Categoria nueva = new Categoria(nombre, raiz);
            raiz.Hijos.InsertarOrdenado(nueva);
            totalCategorias++;
            return nueva;
        }

        /// <summary>
        /// Agrega una subcategoría bajo un padre específico.
        /// La ruta del padre se indica con nombres separados por '>' (ej: "Ciencia > Física").
        /// Si el padre no existe, se crean las categorías intermedias.
        /// </summary>
        public Categoria AgregarSubcategoria(string rutaPadre, string nombreHija)
        {
            Categoria padre = BuscarOCrearPorRuta(rutaPadre);

            Categoria? existente = padre.Hijos.BuscarPorNombre(nombreHija);

            if (existente != null)
            {
                return existente;
            }

            Categoria hija = new Categoria(nombreHija, padre);
            padre.Hijos.InsertarOrdenado(hija);
            totalCategorias++;
            return hija;
        }

        /// <summary>
        /// Busca una categoría por su ruta completa (ej: "Ciencia > Física > Mecánica").
        /// Si no existe, la crea iterativamente.
        /// </summary>
        private Categoria BuscarOCrearPorRuta(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;

            for (int i = 0; i < partes.Length; i++)
            {
                string nombreLimpio = partes[i].Trim();

                if (string.IsNullOrEmpty(nombreLimpio))
                {
                    continue;
                }

                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    encontrada = new Categoria(nombreLimpio, actual);
                    actual.Hijos.InsertarOrdenado(encontrada);
                    totalCategorias++;
                }

                actual = encontrada;
            }

            return actual;
        }

        /// <summary>
        /// Busca una categoría por ruta completa (ej: "Ciencia > Física").
        /// Retorna null si no existe.
        /// </summary>
        public Categoria? BuscarPorRuta(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;

            for (int i = 0; i < partes.Length; i++)
            {
                string nombreLimpio = partes[i].Trim();

                if (string.IsNullOrEmpty(nombreLimpio))
                {
                    continue;
                }

                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    return null;
                }

                actual = encontrada;
            }

            return actual;
        }

        /// <summary>
        /// Elimina una categoría por ruta.
        /// No elimina si tiene libros o subcategorías.
        /// </summary>
        public bool EliminarCategoria(string ruta)
        {
            string[] partes = ruta.Split('>');
            Categoria actual = raiz;

            for (int i = 0; i < partes.Length - 1; i++)
            {
                string nombreLimpio = partes[i].Trim();
                Categoria? encontrada = actual.Hijos.BuscarPorNombre(nombreLimpio);

                if (encontrada == null)
                {
                    return false;
                }

                actual = encontrada;
            }

            string nombreEliminar = partes[partes.Length - 1].Trim();
            Categoria? aEliminar = actual.Hijos.BuscarPorNombre(nombreEliminar);

            if (aEliminar == null)
            {
                return false;
            }

            // No eliminar si tiene contenido
            if (!aEliminar.Hijos.EstaVacia() || aEliminar.Libros.ObtenerCantidad() > 0)
            {
                return false;
            }

            actual.Hijos.EliminarPorNombre(nombreEliminar);
            totalCategorias--;
            return true;
        }

        /// <summary>
        /// Retorna todas las categorías raíz (directamente bajo la raíz).
        /// </summary>
        public Categoria[] ObtenerCategoriasRaiz()
        {
            return raiz.Hijos.ObtenerTodas();
        }

        /// <summary>
        /// Retorna todas las categorías del árbol en orden preorden (profundidad primero).
        /// Útil para mostrar la estructura completa.
        /// </summary>
        public Categoria[] ObtenerTodasLasCategorias()
        {
            ListaEnlazadaSimple<Categoria> lista = new ListaEnlazadaSimple<Categoria>();
            RecorrerPreorden(raiz, lista);
            return lista.ObtenerArray();
        }

        private void RecorrerPreorden(Categoria nodo, ListaEnlazadaSimple<Categoria> lista)
        {
            // No incluir la raíz virtual en la lista
            if (nodo != raiz)
            {
                lista.Agregar(nodo);
            }

            Categoria[] hijos = nodo.Hijos.ObtenerTodas();
            for (int i = 0; i < hijos.Length; i++)
            {
                RecorrerPreorden(hijos[i], lista);
            }
        }

        /// <summary>
        /// Genera una representación en texto de toda la estructura de categorías.
        /// Muestra la jerarquía con indentación.
        /// </summary>
        public string GenerarEstructuraTexto()
        {
            return GenerarTextoNodo(raiz, "", true, true);
        }

        /// <summary>
        /// Genera la estructura en texto a partir de una categoría específica.
        /// </summary>
        public string GenerarEstructuraDesde(string ruta)
        {
            Categoria? categoria = BuscarPorRuta(ruta);

            if (categoria == null)
            {
                return "Categoría no encontrada: " + ruta;
            }

            return GenerarTextoNodo(categoria, "", true, false);
        }

        private string GenerarTextoNodo(Categoria nodo, string prefijo, bool esUltimo, bool esRaiz)
        {
            string resultado = "";

            if (!esRaiz)
            {
                resultado += prefijo + (esUltimo ? "└── " : "├── ")
                    + nodo.Nombre
                    + $" [{nodo.Libros.ObtenerCantidad()} libros]\n";
            }

            string nuevoPrefijo = esRaiz ? "" : prefijo + (esUltimo ? "    " : "│   ");

            Categoria[] hijos = nodo.Hijos.ObtenerTodas();

            for (int i = 0; i < hijos.Length; i++)
            {
                bool esUltimoHijo = (i == hijos.Length - 1);
                resultado += GenerarTextoNodo(hijos[i], nuevoPrefijo, esUltimoHijo, false);
            }

            return resultado;
        }

        public int ObtenerTotalCategorias()
        {
            return totalCategorias;
        }
    }

    /// <summary>
    /// Lista enlazada genérica simple para uso interno del árbol de categorías.
    /// Permitida ya que es un TDA implementado por el estudiante.
    /// </summary>
    public class ListaEnlazadaSimple<T>
    {
        private NodoSimple<T>? cabeza;
        private int contador;

        public ListaEnlazadaSimple()
        {
            cabeza = null;
            contador = 0;
        }

        public void Agregar(T elemento)
        {
            NodoSimple<T> nuevo = new NodoSimple<T>(elemento);

            if (cabeza == null)
            {
                cabeza = nuevo;
            }
            else
            {
                NodoSimple<T> actual = cabeza;
                while (actual.Siguiente != null)
                {
                    actual = actual.Siguiente;
                }
                actual.Siguiente = nuevo;
            }

            contador++;
        }

        public T[] ObtenerArray()
        {
            T[] array = new T[contador];
            NodoSimple<T>? actual = cabeza;
            int i = 0;

            while (actual != null)
            {
                array[i] = actual.Dato;
                i++;
                actual = actual.Siguiente;
            }

            return array;
        }

        public int ObtenerCantidad()
        {
            return contador;
        }
    }

    /// <summary>
    /// Nodo genérico para lista enlazada simple.
    /// </summary>
    public class NodoSimple<T>
    {
        public T Dato { get; set; }
        public NodoSimple<T>? Siguiente { get; set; }

        public NodoSimple(T dato)
        {
            Dato = dato;
            Siguiente = null;
        }
    }
}
