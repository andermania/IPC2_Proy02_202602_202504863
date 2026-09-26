namespace GestiónDeBiblioteca.TDA
{
    /// <summary>
    /// Nodo concreto para lista enlazada simple de cadenas (sin genéricos).
    /// </summary>
    public class NodoSimpleCadena
    {
        public string Dato { get; set; }
        public NodoSimpleCadena? Siguiente { get; set; }

        public NodoSimpleCadena(string dato)
        {
            Dato = dato;
            Siguiente = null;
        }
    }

    /// <summary>
    /// Lista enlazada simple concreta para cadenas (TDA propio, sin genéricos).
    /// Se usa para acumular errores de carga XML.
    /// </summary>
    public class ListaSimpleCadenas
    {
        private NodoSimpleCadena? cabeza;
        private int contador;

        public ListaSimpleCadenas()
        {
            cabeza = null;
            contador = 0;
        }

        public void Agregar(string elemento)
        {
            NodoSimpleCadena nuevo = new NodoSimpleCadena(elemento);

            if (cabeza == null)
            {
                cabeza = nuevo;
            }
            else
            {
                NodoSimpleCadena actual = cabeza;
                while (actual.Siguiente != null)
                {
                    actual = actual.Siguiente;
                }
                actual.Siguiente = nuevo;
            }

            contador++;
        }

        public string[] ObtenerArray()
        {
            string[] array = new string[contador];
            NodoSimpleCadena? actual = cabeza;
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
}
