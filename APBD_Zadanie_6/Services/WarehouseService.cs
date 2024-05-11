using APBD_Task_6.Models;
using System.Data.SqlClient;
using System;
using System.Data.SqlTypes;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace Zadanie5.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IConfiguration _configuration;

        public WarehouseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<int> AddProduct(ProductWarehouse productWarehouse)
        {
            var connectionString = _configuration.GetConnectionString("Database");
            using var connection = new SqlConnection(connectionString);
            using var cmd = new SqlCommand();

            cmd.Connection = connection;
            await connection.OpenAsync();

            cmd.CommandText = "SELECT TOP 1 [ORDER].IdOrder FROM [Order] " +
               "LEFT JOIN Product_Warehouse ON [Order].IdOrder = Product_Warehouse.IdOrder" +
               "WHERE [Order].IdProduct = @IdProduct " +
               "AND [Order].Amout = @Amount" +
               "AND Product_Warehouse.IdProductWarehouse IS NULL " +
               "AND [Order].CreatedAt < @CreatedAt";

            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);
            cmd.Parameters.AddWithValue("Amount", productWarehouse.Amount);
            cmd.Parameters.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
            var reader = await.cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();
            await reader.ReadAsync();

            int idOrder = int.Parse(reader["IdOrder"].ToString());
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);

            reader = await.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();

            await reader.ReadAsync();

            double price = double.Parse(reader["Price"].toString());
            await reader.CloseAsync();

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT IdWarehouse FROM Warehouse WHERE WareHouse = @IdWarehouse"
            cmd.Parameters.AddWithValue("IdProduct", productWarehouse.IdProduct);

            reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows) throw new Exception();

            await reader.ReadAsync();

            cmd.Parameters.Clear();

            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            cmd.Transaction = transaction;

            try
            {
                cmd.CommandText = "UPDATE [Order] SET FulfillAt = @CreatedAt WHERE IdOrder = @IdOrder";
                cmd.Parameter.AddWithValue("CreatedAt", productWarehouse.CreatedAt);
                cmd.Parameter.AddWithValue("IdOrder", idOrder);

                int rowsUpdated = await cmd.ExecuteNonQueryAsync();

                if (rowsUpdated < 1) throw new Exception();

                cmd.Parameters.Clear();

                cmd.CommandText = "INSERT INTO Product_Warehouse(IdWarehouse,IdProduct,IdOrder,Amount,Price,CreatedAt" +
                    "$VALUSE(@IdWarehouse,@IdProduct,@IdOrder,@Amount,@Amount*{price},@CreatedAt)";
                cmd.Parameter.AddWithValue("IdWarehouse", productWarehouse.IdWarehouse);
                cmd.Parameter.AddWithValue("IdProduct", productWarehouse.IdProduct)
                cmd.Parameter.AddWithValue("IdOrder", idOrder);
                cmd.Parameter.AddWithValue("Amount", productWarehouse.Amount);

                int rowsUpdated = await cmd.ExecuteNonQueryAsync();
                
                if (rowsUpdated < 1) throw new Exception();

                await transaction.CommitAsync();

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw new Exception();
            }

            cmd.Parameters.Clear();

            cmd.CommandText = "SELECT TOP 1 IdProductWarehouse From Product_Warehouse ORDER BY IdProductWarehouse";

            reader = await cmd.ExecuteReaderAsync();

            int idProductWarehouse = int.Parse(reader["IdProductWarehouse"].ToString());
            await reader.CloseAsync();

            await connection.CloseAsync();

            return idProductWarehouse;
        }
    }
}