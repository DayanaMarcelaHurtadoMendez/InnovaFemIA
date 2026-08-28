using System.Globalization;
using InnovaFemIA.Web.Features.Shared.Entities;

namespace InnovaFemIA.Web.Features.Shared.Services;

/// <summary>
/// Servicio astronómico local de alta precisión para el cálculo de fases lunares,
/// porcentaje de iluminación, edad lunar y calendario mensual para el Municipio de Caldono, Cauca, Colombia.
/// Implementa los algoritmos astronómicos de Jean Meeus (Astronomical Algorithms Cap. 48).
/// 100% offline, opera en el backend sin requerir servicios externos ni conexión a internet.
/// </summary>
public static class ServicioCalculoLunar
{
    // Coordenadas geográficas oficiales de Caldono, Cauca, Colombia
    public const double LatitudCaldono = 2.7978;
    public const double LongitudCaldono = -76.4828;
    public const int AltitudMsnmCaldono = 1800;
    public const string Municipio = "Caldono";
    public const string Departamento = "Cauca";
    public const string Pais = "Colombia";
    public const string ZonaHoraria = "America/Bogota (UTC-5)";
    private static readonly TimeSpan OffsetColombia = TimeSpan.FromHours(-5);

    // Duración media del mes sinódico lunar en días (29 días, 12 horas, 44 minutos, 2.9 segundos)
    public const double PeriodoSinodico = 29.530588853;

    /// <summary>
    /// Calcula el estado lunar completo actual o para una fecha específica en Caldono, Cauca.
    /// </summary>
    public static ResultadoCalculoLunar CalcularFase(DateTime? fechaUtc = null)
    {
        var utc = fechaUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        var horaLocal = utc.Add(OffsetColombia);

        // 1. Cálculo de Día Juliano (JD) y Siglos Julianos (T) desde J2000.0
        var jd = ToJulianDay(utc);
        var t = (jd - 2451545.0) / 36525.0;

        // 2. Argumentos astronómicos fundamentales de Meeus (en grados)
        // Elongación media de la Luna respecto al Sol
        var d = Modulo360(297.8501921 + 445267.1114034 * t - 0.0018819 * t * t + t * t * t / 545868.0);
        // Anomalía media del Sol
        var m = Modulo360(357.5291092 + 35999.0502909 * t - 0.0001536 * t * t + t * t * t / 24490000.0);
        // Anomalía media de la Luna
        var mPrime = Modulo360(134.9633964 + 477198.8675055 * t + 0.0087414 * t * t + t * t * t / 69699.0);

        // Convertir a radianes para evaluación trigonométrica
        var dRad = d * Math.PI / 180.0;
        var mRad = m * Math.PI / 180.0;
        var mPrimeRad = mPrime * Math.PI / 180.0;

        // 3. Ángulo de fase lunar geocéntrico (i) con correcciones de perturbación (Meeus Cap. 48)
        var iDeg = 180.0 - d
                   - 6.289 * Math.Sin(mPrimeRad)
                   + 2.100 * Math.Sin(mRad)
                   - 1.274 * Math.Sin(2.0 * dRad - mPrimeRad)
                   - 0.658 * Math.Sin(2.0 * dRad)
                   - 0.214 * Math.Sin(2.0 * mPrimeRad)
                   - 0.110 * Math.Sin(dRad);

        var iRad = iDeg * Math.PI / 180.0;

        // 4. Fracción de iluminación (k) entre 0.0 y 1.0
        var k = (1.0 + Math.Cos(iRad)) / 2.0;
        k = Math.Clamp(k, 0.0, 1.0);

        // 5. Fracción de lunación y edad lunar en días
        var fraccionLunacion = d / 360.0;
        var edadDias = fraccionLunacion * PeriodoSinodico;

        // 6. Clasificación en las 4 fases lunares tradicionales reconocidas en el campo
        // Cuadrante 1 (0° a <90°): Luna Nueva (creciendo hacia Cuarto Creciente)
        // Cuadrante 2 (90° a <180°): Cuarto Creciente (creciendo hacia Luna Llena)
        // Cuadrante 3 (180° a <270°): Luna Llena (menguando hacia Cuarto Menguante)
        // Cuadrante 4 (270° a <360°): Cuarto Menguante (menguando hacia Luna Nueva)
        FaseLunar faseEnum;
        string nombreFase;
        string iconoFase;
        string descripcionVisual;

        if (d < 90.0)
        {
            faseEnum = FaseLunar.LunaNueva;
            nombreFase = "Luna Nueva";
            descripcionVisual = "Savia en las raíces (fase de reposo y mantenimiento de terreno)";
        }
        else if (d < 180.0)
        {
            faseEnum = FaseLunar.CuartoCrediente;
            nombreFase = "Cuarto Creciente";
            descripcionVisual = "Savia en ascenso hacia tallos y hojas (fase de siembra y esquejes)";
        }
        else if (d < 270.0)
        {
            faseEnum = FaseLunar.LunaLlena;
            nombreFase = "Luna Llena";
            descripcionVisual = "Savia en hojas, flores y frutos (fase de cosecha de café y frutos maduros)";
        }
        else
        {
            faseEnum = FaseLunar.CuartoMenguante;
            nombreFase = "Cuarto Menguante";
            descripcionVisual = "Savia en descenso hacia las raíces (corte de madera, guadua y trasplantes)";
        }

        // Determinar el ícono exacto según la elongación para que coincida con el % de iluminación real
        if (d < 10.0 || d >= 350.0) iconoFase = "🌑";
        else if (d >= 10.0 && d < 80.0) iconoFase = "🌒";
        else if (d >= 80.0 && d < 100.0) iconoFase = "🌓";
        else if (d >= 100.0 && d < 170.0) iconoFase = "🌔";
        else if (d >= 170.0 && d < 190.0) iconoFase = "🌕";
        else if (d >= 190.0 && d < 260.0) iconoFase = "🌖";
        else if (d >= 260.0 && d < 280.0) iconoFase = "🌗";
        else iconoFase = "🌘";

        // 7. Cálculo de tiempo hasta el próximo cambio de fase principal
        var (proximaFase, diasRestantes, fechaProximaUtc) = CalcularProximaFase(d, utc);
        var fechaProximaLocal = fechaProximaUtc.Add(OffsetColombia);

        // 8. Calendario de las 4 fases principales del mes actual
        var calendarioMes = CalcularCalendarioMes(horaLocal.Year, horaLocal.Month);

        return new ResultadoCalculoLunar
        {
            FaseEnum = faseEnum,
            NombreFase = nombreFase,
            Icono = iconoFase,
            DescripcionVisual = descripcionVisual,
            Iluminacion = Math.Round((decimal)k, 4),
            PorcentajeIluminacion = $"{Math.Round(k * 100.0, 1)}%",
            EdadDias = Math.Round((decimal)edadDias, 1),
            DiasParaProximoCambio = Math.Round((decimal)diasRestantes, 1),
            HorasParaProximoCambio = (int)Math.Round(diasRestantes * 24.0),
            NombreProximaFase = proximaFase,
            FechaProximaFaseLocal = fechaProximaLocal,
            FechaCalculoUtc = utc,
            FechaCalculoLocal = horaLocal,
            CalendarioMes = calendarioMes,
            Ubicacion = new UbicacionObservacion
            {
                Municipio = Municipio,
                Departamento = Departamento,
                Pais = Pais,
                Latitud = LatitudCaldono,
                Longitud = LongitudCaldono,
                AltitudMsnm = AltitudMsnmCaldono,
                ZonaHoraria = ZonaHoraria
            }
        };
    }

    private static (string NombreFase, double DiasRestantes, DateTime FechaProximaUtc) CalcularProximaFase(double d, DateTime utc)
    {
        double targetD;
        string nombre;

        if (d < 90.0)
        {
            targetD = 90.0;
            nombre = "Cuarto Creciente";
        }
        else if (d < 180.0)
        {
            targetD = 180.0;
            nombre = "Luna Llena";
        }
        else if (d < 270.0)
        {
            targetD = 270.0;
            nombre = "Cuarto Menguante";
        }
        else
        {
            targetD = 360.0;
            nombre = "Luna Nueva";
        }

        var gradosRestantes = targetD - d;
        if (gradosRestantes <= 0) gradosRestantes += 360.0;

        // La elongación avanza aprox. 360 / 29.530588853 = 12.190749° por día
        var diasRestantes = gradosRestantes / (360.0 / PeriodoSinodico);
        var fechaProxima = utc.AddDays(diasRestantes);

        return (nombre, diasRestantes, fechaProxima);
    }

    /// <summary>
    /// Genera las 4 fases principales del mes con fechas y horas precisas en tiempo local de Colombia (UTC-5).
    /// </summary>
    public static List<FaseCalendarioItem> CalcularCalendarioMes(int anio, int mes)
    {
        var lista = new List<FaseCalendarioItem>();
        var inicioMesUtc = new DateTime(anio, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMesUtc = inicioMesUtc.AddMonths(1).AddDays(1);

        var targets = new (double TargetD, FaseLunar Fase, string Nombre, string Icono)[]
        {
            (0.0, FaseLunar.LunaNueva, "Luna Nueva", "🌑"),
            (90.0, FaseLunar.CuartoCrediente, "Cuarto Creciente", "🌓"),
            (180.0, FaseLunar.LunaLlena, "Luna Llena", "🌕"),
            (270.0, FaseLunar.CuartoMenguante, "Cuarto Menguante", "🌗")
        };

        var cur = inicioMesUtc;
        while (cur < finMesUtc)
        {
            var jd = ToJulianDay(cur);
            var t = (jd - 2451545.0) / 36525.0;
            var d = Modulo360(297.8501921 + 445267.1114034 * t);

            foreach (var target in targets)
            {
                var diff = Math.Abs(d - target.TargetD);
                if (diff > 180.0) diff = 360.0 - diff;

                // Dentro de ventana de 2 horas (aprox 1.0 grado)
                if (diff < 1.1)
                {
                    var fechaLocal = cur.Add(OffsetColombia);
                    if (fechaLocal.Month == mes && !lista.Any(x => x.Fase == target.Fase && Math.Abs((x.FechaHoraLocal - fechaLocal).TotalDays) < 6.0))
                    {
                        lista.Add(new FaseCalendarioItem
                        {
                            Fase = target.Fase,
                            NombreFase = target.Nombre,
                            Icono = target.Icono,
                            FechaHoraLocal = fechaLocal,
                            FechaFormateada = fechaLocal.ToString("dddd, dd 'de' MMMM", new CultureInfo("es-CO")),
                            HoraFormateada = fechaLocal.ToString("hh:mm tt", CultureInfo.InvariantCulture)
                        });
                    }
                }
            }

            cur = cur.AddHours(2);
        }

        return lista.OrderBy(x => x.FechaHoraLocal).ToList();
    }

    private static double ToJulianDay(DateTime utc)
    {
        var year = utc.Year;
        var month = utc.Month;
        var day = utc.Day + (utc.Hour + (utc.Minute + utc.Second / 60.0) / 60.0) / 24.0;

        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        var a = Math.Floor(year / 100.0);
        var b = 2 - a + Math.Floor(a / 4.0);

        return Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + b - 1524.5;
    }

    private static double Modulo360(double valor)
    {
        var r = valor % 360.0;
        return r < 0 ? r + 360.0 : r;
    }
}

public class ResultadoCalculoLunar
{
    public FaseLunar FaseEnum { get; set; }
    public string NombreFase { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌒";
    public string DescripcionVisual { get; set; } = string.Empty;
    public decimal Iluminacion { get; set; }
    public string PorcentajeIluminacion { get; set; } = "0%";
    public decimal EdadDias { get; set; }
    public decimal DiasParaProximoCambio { get; set; }
    public int HorasParaProximoCambio { get; set; }
    public string NombreProximaFase { get; set; } = string.Empty;
    public DateTime FechaProximaFaseLocal { get; set; }
    public DateTime FechaCalculoUtc { get; set; }
    public DateTime FechaCalculoLocal { get; set; }
    public List<FaseCalendarioItem> CalendarioMes { get; set; } = [];
    public UbicacionObservacion Ubicacion { get; set; } = new();
}

public class FaseCalendarioItem
{
    public FaseLunar Fase { get; set; }
    public string NombreFase { get; set; } = string.Empty;
    public string Icono { get; set; } = "🌒";
    public DateTime FechaHoraLocal { get; set; }
    public string FechaFormateada { get; set; } = string.Empty;
    public string HoraFormateada { get; set; } = string.Empty;
}

public class UbicacionObservacion
{
    public string Municipio { get; set; } = "Caldono";
    public string Departamento { get; set; } = "Cauca";
    public string Pais { get; set; } = "Colombia";
    public double Latitud { get; set; } = 2.7978;
    public double Longitud { get; set; } = -76.4828;
    public int AltitudMsnm { get; set; } = 1800;
    public string ZonaHoraria { get; set; } = "UTC-5 (America/Bogota)";
}
