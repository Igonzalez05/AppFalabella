using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ConsoleApp1FALABELLA
{
    class Program
    {
        // TODO: Cambia esta cadena por la de tu servidor
        private const string ConnectionString =
           // "data source=localhost; initial catalog=Prueba_practica; user id=sa; password=admin1234.;Encrypt=True;TrustServerCertificate=True;";
            "data source=138.99.6.164; initial catalog= bd_centure_cloud_v2; user id=sa; password=ToQNGL391#..;Encrypt=True;TrustServerCertificate=True;";

        private const string LocacionesFalabella =
            "676,677,678,679,766,767,768,769,770,771,772,773,774,775,776,777,778,779,780,781,782,783,784,785,786,787,788,789,790,791,792,793,794,795,796,797,798,799,800,801,802,803,804,805,806,807,808,809,810,811,812,813,814,815,816,817,818,819,820,821,822,823,824,825,826,827,828,829,830,831,832,833,834,835,836,837,838,839,840,841,842,843";

        static void Main(string[] args)
        {
            // HOY = fecha base del proceso
            DateTime fechaHoy;
            if (args.Length > 0 && DateTime.TryParse(args[0], out var f))
                fechaHoy = f;
            else
                fechaHoy = DateTime.Today;

            // MAÑANA = inventario nuevo
            DateTime fechaInventarioNuevo = fechaHoy.AddDays(1);

            Console.WriteLine("=== Proceso de inventario Falabella ===");
            Console.WriteLine($"Fecha hoy:              {fechaHoy:yyyy-MM-dd}");
            Console.WriteLine($"Fecha inventario nuevo: {fechaInventarioNuevo:yyyy-MM-dd}");
            Console.WriteLine("----------------------------------------");
      
            

            try
            {
                using (var conexion = new SqlConnection(ConnectionString))
                {
                    conexion.Open();

                    // 1) GUARDAR EN HISTORIAL DE INVENTARIO FALABELLA (HOY)
                    Console.WriteLine(
                        $"[1] Guardar historial de inventario Falabella \"{fechaHoy:dd/MM/yyyy}\"");
                    EjecutarActualizarStandAlone(conexion, fechaHoy);

                    // 2) CIERRE INVENTARIO ACTUAL (mandato abierto hoy)
                    int nMandatoActual = ObtenerMandatoActual(conexion);
                    Console.WriteLine(
                        $"[2] Cierre inventario actual. Mandato: {nMandatoActual}");
                    CambiarEstadoInventario(conexion, nMandatoActual);

                    // 3) CREAR INVENTARIO ACTUAL (nuevo inventario para MAÑANA)
                    Console.WriteLine(
                        $"[3] Crear inventario actual \"{fechaInventarioNuevo:dd/MM/yyyy}\"");
                    CrearInventarioMandatorio(conexion, fechaInventarioNuevo);

                    // 4) ACTUALIZAR MANDATO (fecha_inventario y fecha_estado_inventario)
                    Console.WriteLine("[4] Actualizar fechas del nuevo mandato...");
                    ActualizarFechasMandatoNuevo(conexion, fechaInventarioNuevo);
                }

                Console.WriteLine("=== PROCESO COMPLETADO OK ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR GENERAL ===");
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Presiona una tecla para salir...");
            Console.ReadKey();
        }


        private static void EjecutarActualizarStandAlone(SqlConnection conexion, DateTime fecha)
        {
            Console.WriteLine("   -> Ejecutando spUpt_Actualizar_Falabella_Stand_Alone...");

            using (var cmd = new SqlCommand("[rfid].[spUpt_Actualizar_Falabella_Stand_Alone]", conexion))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@par_Fecha", fecha);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"   -> OK historial inventario guardado para fecha: {fecha:dd/MM/yyyy}");
        }


        private static int ObtenerMandatoActual(SqlConnection conexion)
        {
            Console.WriteLine("   -> Obteniendo número de mandato actual...");

            const string sql = @"
                                SELECT MAX(numero)
                                FROM [rfid].[Head_Inventory_C78_P232]
                                WHERE id_tipo = 1 AND id_estado_inventario = 1;";

            using (var cmd = new SqlCommand(sql, conexion))
            {
                var result = cmd.ExecuteScalar();
                int nMandato = result == DBNull.Value ? 0 : Convert.ToInt32(result);

                Console.WriteLine($"   -> Mandato actual (abierto): {nMandato}");
                return nMandato;
            }
        }


        private static void CambiarEstadoInventario(SqlConnection conexion, int nMandato)
        {
            Console.WriteLine("   -> Cerrando inventario actual...");

            using (var cmd = new SqlCommand("[rfid].[spUpt_nuevo_estado_inventario]", conexion))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@par_idUsuario", 24);
                cmd.Parameters.AddWithValue("@par_idCliente", 78);
                cmd.Parameters.AddWithValue("@par_idProyecto", 232);
                cmd.Parameters.AddWithValue("@par_numeroMandato", nMandato);
                cmd.Parameters.AddWithValue("@par_id_registroInventario", 0);
                cmd.Parameters.AddWithValue("@par_id_nuevo_estado", 2);
                cmd.Parameters.AddWithValue("@par_observaciones", "");

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine("   -> Inventario actual cerrado (estado = 2).");
        }


        private static void CrearInventarioMandatorio(SqlConnection conexion, DateTime fecha)
        {
            Console.WriteLine("   -> Creando nuevo inventario mandatorio...");

            string descripcion = $"Inventario {fecha:dd/MM/yyyy}";

            using (var cmd = new SqlCommand("[rfid].[spIns_Inventario_Mandatorio]", conexion))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@par_idUsuario", 24);
                cmd.Parameters.AddWithValue("@par_idCliente", 78);
                cmd.Parameters.AddWithValue("@par_idProyecto", 232);
                cmd.Parameters.AddWithValue("@par_descripcion", descripcion);
                cmd.Parameters.AddWithValue("@par_locacionAreas", LocacionesFalabella);
                cmd.Parameters.AddWithValue("@par_observaciones", descripcion);
                cmd.Parameters.AddWithValue("@par_tipoInventario", 1);
                cmd.Parameters.AddWithValue("@par_implementacionInventario", 1);
                cmd.Parameters.AddWithValue("@par_items", "");

                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"   -> Inventario mandatorio creado. Fecha: {fecha:dd/MM/yyyy}");
        }

        private static void ActualizarFechasMandatoNuevo(SqlConnection conexion, DateTime fechaInventarioNuevo)
        {
            // Puedes usar DateTime.Now si quieres fecha con hora exacta de ejecución
            DateTime fechaEstado = fechaInventarioNuevo; // o DateTime.Now

            // 1) Obtener mandato NUEVO (el que quedó abierto luego de crear inventario)
            const string sqlGetMandatoNuevo = @"
                        SELECT MAX(numero)
                        FROM [rfid].[Head_Inventory_C78_P232]
                        WHERE id_tipo = 1 AND id_estado_inventario = 1;";

            int nMandatoNuevo;
            using (var cmd = new SqlCommand(sqlGetMandatoNuevo, conexion))
            {
                var result = cmd.ExecuteScalar();
                nMandatoNuevo = result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }

            Console.WriteLine($"   -> Mandato nuevo abierto: {nMandatoNuevo}");

            // 2) UPDATE Head_Inventory_C78_P232.fecha_inventario
            const string sqlUpdateHead = @"
                        UPDATE [rfid].[Head_Inventory_C78_P232]
                        SET fecha_inventario = @par_fecha
                        WHERE id_tipo = 1 AND numero = @numero;";

            using (var cmd = new SqlCommand(sqlUpdateHead, conexion))
            {
                cmd.Parameters.AddWithValue("@par_fecha", fechaEstado);
                cmd.Parameters.AddWithValue("@numero", nMandatoNuevo);
                cmd.ExecuteNonQuery();
            }

            // 3) UPDATE Summary_Inventory_C78_P232.fecha_estado_inventario
            const string sqlUpdateSummary = @"
                        UPDATE [rfid].[Summary_Inventory_C78_P232]
                        SET fecha_estado_inventario = @par_fecha
                        WHERE id_tipo = 1 AND numero = @numero;";

            using (var cmd = new SqlCommand(sqlUpdateSummary, conexion))
            {
                cmd.Parameters.AddWithValue("@par_fecha", fechaEstado);
                cmd.Parameters.AddWithValue("@numero", nMandatoNuevo);
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine(
                $"   -> Fechas actualizadas en mandato {nMandatoNuevo}: {fechaEstado:yyyy-MM-dd HH:mm:ss}");
        }


    }
}
