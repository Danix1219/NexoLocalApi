// Crea las tres bases de nv-00 (FR-005), una por dominio, en el entorno local.
// Idempotente: repetirlo no cambia nada. Mongo materializa una base cuando tiene
// al menos una coleccion, asi que se crea una coleccion de marca por base.

const owners = {
  nexo_identity: 'identity',
  nexo_discovery: 'discovery',
  nexo_engagement: 'engagement',
};

for (const [name, owner] of Object.entries(owners)) {
  const target = db.getSiblingDB(name);

  if (!target.getCollectionNames().includes('_domain')) {
    target.createCollection('_domain');
  }

  // Deja constancia de quien es el unico dominio autorizado a escribir esta base.
  target._domain.replaceOne(
    { _id: 'owner' },
    { _id: 'owner', domain: owner, createdBy: 'nv-00/mongo-init.js' },
    { upsert: true },
  );

  print(`base ${name} lista, dominio ${owner}`);
}

const created = Object.keys(owners).filter((name) =>
  db.getSiblingDB(name).getCollectionNames().includes('_domain'),
);

if (created.length !== 3) {
  throw new Error(`se esperaban 3 bases, hay ${created.length}`);
}

print('mongo-init: 3 bases verificadas');
