// Migration 007: Backfill Login field for existing users
// Users created before Login/Email separation have no Login value.
// This sets Login = Email for any user missing the Login field.

print("=== Migration 007: Backfill User Login field ===");

var configDb = db.getSiblingDB("BalanceLedger_Config");
var users = configDb.getCollection("users");

if (!users) {
    print("[users] Collection not found — skipping.");
} else {
    var result = users.updateMany(
        { $or: [{ Login: null }, { Login: "" }, { Login: { $exists: false } }] },
        [{ $set: { Login: "$Email" } }]
    );
    print("[users] Updated " + result.modifiedCount + " user(s) with Login = Email");
}

print("=== Migration 007 complete ===");
