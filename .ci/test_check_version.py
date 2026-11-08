"""Pruebas del validador de VERSION. No tocan red, disco de produccion ni secretos."""
import importlib.util
import pathlib
import unittest

RUTA = pathlib.Path(__file__).with_name("check-version.py")
_spec = importlib.util.spec_from_file_location("check_version", RUTA)
check_version = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(check_version)

validate = check_version.validate


class VersionValida(unittest.TestCase):
    def test_version_simple(self):
        self.assertIn("0.2.0", validate("0.2.0", None))

    def test_tolera_espacios_y_salto_final(self):
        self.assertIn("1.0.0", validate("  1.0.0\n", None))

    def test_candidato_de_entrega(self):
        self.assertIn("0.2.0-rc.1", validate("0.2.0-rc.1", None))

    def test_cero_es_valido(self):
        self.assertIn("0.0.0", validate("0.0.0", None))


class VersionInvalida(unittest.TestCase):
    def rechaza(self, texto, rama=None):
        with self.assertRaises(ValueError):
            validate(texto, rama)

    def test_vacio(self):
        self.rechaza("")

    def test_solo_espacios(self):
        self.rechaza("   \n")

    def test_dos_lineas(self):
        self.rechaza("0.2.0\n0.3.0\n")

    def test_sin_parche(self):
        self.rechaza("0.2")

    def test_con_prefijo_v(self):
        self.rechaza("v0.2.0")

    def test_ceros_a_la_izquierda(self):
        self.rechaza("01.2.0")

    def test_texto_libre(self):
        self.rechaza("ultima")

    def test_prerelease_en_mayusculas(self):
        # El nombre de la rama solo admite minusculas; si aqui se permitieran, nunca coincidirian.
        self.rechaza("0.2.0-RC.1")


class CoherenciaConLaRama(unittest.TestCase):
    def test_rama_release_que_coincide(self):
        self.assertIn("0.2.0-rc.1", validate("0.2.0-rc.1", "release/0.2.0-rc.1"))

    def test_rama_con_candidato_y_version_limpia(self):
        # El sufijo -rc.N cuenta las vueltas de la entrega, no la version entregada.
        self.assertIn("0.2.0", validate("0.2.0", "release/0.2.0-rc.1"))

    def test_rama_con_candidato_y_nucleo_distinto(self):
        with self.assertRaises(ValueError):
            validate("0.2.0", "release/0.3.0-rc.1")

    def test_rama_release_sin_version_valida(self):
        with self.assertRaises(ValueError):
            validate("0.2.0", "release/caliente")

    def test_rama_release_que_no_coincide(self):
        with self.assertRaises(ValueError):
            validate("0.3.0", "release/0.2.0-rc.1")

    def test_rama_de_trabajo_no_exige_coincidencia(self):
        # En una rama de trabajo la version de entrega todavia no esta decidida.
        self.assertIn("0.2.0", validate("0.2.0", "feature/NEX-10-lo-que-sea"))

    def test_develop_no_exige_coincidencia(self):
        self.assertIn("0.2.0", validate("0.2.0", "develop"))

    def test_sin_rama_conocida(self):
        self.assertIn("0.2.0", validate("0.2.0", None))


class LecturaDelEvento(unittest.TestCase):
    def test_sin_ruta(self):
        self.assertIsNone(check_version.rama_del_evento(None))

    def test_ruta_inexistente(self):
        self.assertIsNone(check_version.rama_del_evento("no-existe.json"))


if __name__ == "__main__":
    unittest.main()
