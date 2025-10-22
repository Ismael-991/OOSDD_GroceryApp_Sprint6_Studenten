using Grocery.Core.Data.Helpers;
using Grocery.Core.Interfaces.Repositories;
using Grocery.Core.Models;
using Microsoft.Data.Sqlite;

namespace Grocery.Core.Data.Repositories
{
    /// <summary>
    /// Repository voor het beheren van boodschappenlijstitems in de SQLite-database.
    /// Verzorgt CRUD-operaties en initialisatie van de bijbehorende tabel.
    /// </summary>
    public class GroceryListItemsRepository : DatabaseConnection, IGroceryListItemsRepository
    {
        private readonly List<GroceryListItem> groceryListItems = [];

        /// <summary>
        /// Initialiseert een nieuwe instantie van <see cref="GroceryListItemsRepository"/>.
        /// Maakt de tabel 'GroceryListItem' aan indien deze niet bestaat en voegt seed-data toe.
        /// Laadt daarna alle items in het lokale geheugen.
        /// </summary>
        public GroceryListItemsRepository()
        {
            CreateTable(@"CREATE TABLE IF NOT EXISTS GroceryListItem (
                            [Id] INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                            [GroceryListId] INTEGER NOT NULL,
                            [ProductId] INTEGER NOT NULL,
                            [Amount] INTEGER NOT NULL,
                            FOREIGN KEY(GroceryListId) REFERENCES GroceryList(Id) ON DELETE CASCADE,
                            FOREIGN KEY(ProductId) REFERENCES Product(Id) ON DELETE CASCADE)");

            List<string> insertQueries = [
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(1, 1, 1, 3)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(2, 1, 2, 1)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(3, 1, 3, 4)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(4, 2, 1, 2)",
                @"INSERT OR IGNORE INTO GroceryListItem(Id, GroceryListId, ProductId, Amount) VALUES(5, 2, 2, 5)"
            ];
            InsertMultipleWithTransaction(insertQueries);
            GetAll();
        }

        /// <summary>
        /// Haalt alle boodschappenlijstitems op uit de database en slaat ze lokaal op.
        /// </summary>
        /// <returns>Een lijst met alle <see cref="GroceryListItem"/> objecten.</returns>
        public List<GroceryListItem> GetAll()
        {
            groceryListItems.Clear();
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    groceryListItems.Add(new GroceryListItem(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return groceryListItems;
        }

        /// <summary>
        /// Haalt alle boodschappenlijstitems op die behoren tot een specifieke boodschappenlijst.
        /// </summary>
        /// <param name="groceryListId">De unieke ID van de boodschappenlijst.</param>
        /// <returns>Een lijst met <see cref="GroceryListItem"/> objecten die bij de opgegeven lijst horen.</returns>
        public List<GroceryListItem> GetAllOnGroceryListId(int groceryListId)
        {
            List<GroceryListItem> items = [];
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE GroceryListId = @GroceryListId";
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                command.Parameters.AddWithValue("@GroceryListId", groceryListId);
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    items.Add(new GroceryListItem(id, groceryListId, productId, amount));
                }
            }
            CloseConnection();
            return items;
        }

        /// <summary>
        /// Voegt een nieuw boodschappenlijstitem toe aan de database.
        /// De ID wordt automatisch gegenereerd door de database.
        /// </summary>
        /// <param name="item">Het <see cref="GroceryListItem"/> om toe te voegen (zonder geldige ID).</param>
        /// <returns>Het toegevoegde item, inclusief de door de database toegewezen ID.</returns>
        public GroceryListItem Add(GroceryListItem item)
        {
            string insertQuery = "INSERT INTO GroceryListItem(GroceryListId, ProductId, Amount) VALUES(@GroceryListId, @ProductId, @Amount); SELECT last_insert_rowid();";
            OpenConnection();
            using (SqliteCommand command = new(insertQuery, Connection))
            {
                command.Parameters.AddWithValue("@GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                command.Parameters.AddWithValue("@Amount", item.Amount);

                item.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            CloseConnection();
            return item;
        }

        /// <summary>
        /// Verwijdert een bestaand boodschappenlijstitem uit de database.
        /// </summary>
        /// <param name="item">Het <see cref="GroceryListItem"/> dat verwijderd moet worden.</param>
        /// <returns>Het verwijderde item als de operatie succesvol was; anders <c>null</c>.</returns>
        public GroceryListItem? Delete(GroceryListItem item)
        {
            string deleteQuery = "DELETE FROM GroceryListItem WHERE Id = @Id;";
            OpenConnection();
            using (SqliteCommand command = new(deleteQuery, Connection))
            {
                command.Parameters.AddWithValue("@Id", item.Id);
                int rowsAffected = command.ExecuteNonQuery();
                CloseConnection();
                return rowsAffected > 0 ? item : null;
            }
        }

        /// <summary>
        /// Haalt een specifiek boodschappenlijstitem op op basis van zijn unieke ID.
        /// </summary>
        /// <param name="id">De unieke ID van het item.</param>
        /// <returns>Het gevonden <see cref="GroceryListItem"/> of <c>null</c> als het niet bestaat.</returns>
        public GroceryListItem? Get(int id)
        {
            string selectQuery = "SELECT Id, GroceryListId, ProductId, Amount FROM GroceryListItem WHERE Id = @Id";
            GroceryListItem? item = null;
            OpenConnection();
            using (SqliteCommand command = new(selectQuery, Connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    int groceryListId = reader.GetInt32(1);
                    int productId = reader.GetInt32(2);
                    int amount = reader.GetInt32(3);
                    item = new GroceryListItem(id, groceryListId, productId, amount);
                }
            }
            CloseConnection();
            return item;
        }

        /// <summary>
        /// Werkt een bestaand boodschappenlijstitem bij in de database.
        /// </summary>
        /// <param name="item">Het bij te werken <see cref="GroceryListItem"/> (moet een geldige ID bevatten).</param>
        /// <returns>Het bijgewerkte item.</returns>
        public GroceryListItem? Update(GroceryListItem item)
        {
            string updateQuery = "UPDATE GroceryListItem SET GroceryListId = @GroceryListId, ProductId = @ProductId, Amount = @Amount WHERE Id = @Id";
            OpenConnection();
            using (SqliteCommand command = new(updateQuery, Connection))
            {
                command.Parameters.AddWithValue("@Id", item.Id);
                command.Parameters.AddWithValue("@GroceryListId", item.GroceryListId);
                command.Parameters.AddWithValue("@ProductId", item.ProductId);
                command.Parameters.AddWithValue("@Amount", item.Amount);

                int rowsAffected = command.ExecuteNonQuery();
            }
            CloseConnection();
            return item;
        }
    }
}