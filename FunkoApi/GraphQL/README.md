# 🚀 Guía Completa de GraphQL - FunkoAPI

URL del endpoint GraphQL: **http://localhost:8080/graphql/**

## 📌 Tabla de Contenidos
1. [Queries (Consultas)](#queries)
2. [Mutations (Modificaciones)](#mutations)
3. [Subscriptions (Tiempo Real)](#subscriptions)
4. [Introspection (Exploración del Schema)](#introspection)

---

## 🔍 QUERIES (Consultas)

### 1. Obtener todos los Funkos

```graphql
query {
  allFunkos {
    id
    nombre
    precio
    imagen
    createdAt
    updatedAt
    category {
      id
      nombre
    }
  }
}
```

### 2. Funkos con paginación

```graphql
query {
  allFunkosPaged(first: 5) {
    pageInfo {
      hasNextPage
      hasPreviousPage
      startCursor
      endCursor
    }
    edges {
      cursor
      node {
        id
        nombre
        precio
        category {
          nombre
        }
      }
    }
  }
}
```

### 3. Obtener Funko por ID

```graphql
query {
  funkoById(id: 1) {
    id
    nombre
    precio
    categoryId
    imagen
    createdAt
    updatedAt
    category {
      id
      nombre
    }
  }
}
```

### 4. Obtener todas las Categorías

```graphql
query {
  allCategories {
    id
    nombre
    createdAt
    updatedAt
  }
}
```

### 5. Categorías con paginación

```graphql
query {
  allCategoriesPaged(first: 3, after: null) {
    pageInfo {
      hasNextPage
      hasPreviousPage
    }
    edges {
      node {
        id
        nombre
      }
    }
  }
}
```

### 6. Obtener Categoría por ID

```graphql
query {
  categoryById(id: "722f9661-8631-419b-8903-34e9e0339d01") {
    id
    nombre
    createdAt
    updatedAt
  }
}
```

### 7. Query con Proyección (seleccionar solo campos específicos)

```graphql
query {
  allFunkos {
    nombre
    precio
  }
}
```

### 8. Query compleja con múltiples consultas

```graphql
query {
  funkos: allFunkos {
    id
    nombre
    precio
    category {
      nombre
    }
  }
  categories: allCategories {
    id
    nombre
  }
}
```

---

## ✏️ MUTATIONS (Modificaciones)

### 1. Crear un nuevo Funko

```graphql
mutation {
  createFunko(
    input: {
      nombre: "Bulbasaur"
      categoria: "POKEMON"
      precio: 13.99
      imagen: "bulbasaur.png"
    }
  ) {
    id
    nombre
    precio
    categoria
    imagen
  }
}
```

### 2. Actualizar un Funko (PUT - reemplazo completo)

```graphql
mutation {
  updateFunko(
    id: 1
    input: {
      nombre: "Pikachu Shiny"
      categoria: "POKEMON"
      precio: 15.99
      imagen: "pikachu-shiny.png"
    }
  ) {
    id
    nombre
    precio
    categoria
    imagen
  }
}
```

### 3. Actualización parcial (PATCH)

```graphql
mutation {
  patchFunko(
    id: 2
    input: {
      precio: 14.50
    }
  ) {
    id
    nombre
    precio
    categoria
  }
}
```

### 4. Actualizar solo el nombre (PATCH)

```graphql
mutation {
  patchFunko(
    id: 3
    input: {
      nombre: "Iron Man Mark 50"
    }
  ) {
    id
    nombre
    categoria
  }
}
```

### 5. Eliminar un Funko

```graphql
mutation {
  deleteFunko(id: 10) {
    id
    nombre
    precio
    categoria
  }
}
```

### 6. Crear múltiples Funkos en una sola petición

```graphql
mutation {
  funko1: createFunko(
    input: {
      nombre: "Squirtle"
      categoria: "POKEMON"
      precio: 12.99
      imagen: "squirtle.png"
    }
  ) {
    id
    nombre
    categoria
  }
  
  funko2: createFunko(
    input: {
      nombre: "Thor (Endgame)"
      categoria: "MARVEL"
      precio: 16.50
      imagen: "thor.png"
    }
  ) {
    id
    nombre
    categoria
  }
}
```

---

## 📡 SUBSCRIPTIONS (Tiempo Real)

> **Nota**: Las subscriptions requieren WebSocket. Usa un cliente GraphQL que soporte subscriptions como GraphQL Playground, Apollo Studio, o Altair.

### 1. Escuchar creaciones de Funkos

```graphql
subscription {
  onCreatedFunko {
    id
    nombre
    categoria
    precio
    createdAt
  }
}
```

### 2. Escuchar actualizaciones de Funkos

```graphql
subscription {
  onUpdatedFunko {
    id
    nombre
    categoria
    precio
    updatedAt
  }
}
```

### 3. Escuchar eliminaciones de Funkos

```graphql
subscription {
  onDeletedFunko {
    id
    nombre
    categoria
    precio
    deletedAt
  }
}
```

### 4. Múltiples subscriptions simultáneas

```graphql
subscription {
  created: onCreatedFunko {
    id
    nombre
    precio
  }
  updated: onUpdatedFunko {
    id
    nombre
    precio
  }
  deleted: onDeletedFunko {
    id
    nombre
  }
}
```

---

## 🔬 INTROSPECTION (Exploración del Schema)

### 1. Ver todos los tipos disponibles

```graphql
query {
  __schema {
    types {
      name
      kind
      description
    }
  }
}
```

### 2. Ver detalles del tipo Funko

```graphql
query {
  __type(name: "Funko") {
    name
    description
    fields {
      name
      type {
        name
        kind
      }
      description
    }
  }
}
```

### 3. Ver todas las queries disponibles

```graphql
query {
  __schema {
    queryType {
      fields {
        name
        description
        args {
          name
          type {
            name
            kind
          }
        }
      }
    }
  }
}
```

### 4. Ver todas las mutations disponibles

```graphql
query {
  __schema {
    mutationType {
      fields {
        name
        description
        args {
          name
          type {
            name
          }
        }
      }
    }
  }
}
```

### 5. Ver todas las subscriptions disponibles

```graphql
query {
  __schema {
    subscriptionType {
      fields {
        name
        description
      }
    }
  }
}
```

---

## 🧪 Casos de Prueba Recomendados

### Test 1: Crear y verificar
```graphql
# Paso 1: Crear
mutation {
  createFunko(
    input: {
      nombre: "Test Funko"
      categoria: "TERROR"
      precio: 9.99
      imagen: "test.png"
    }
  ) {
    id
    nombre
  }
}

# Paso 2: Verificar (usa el ID del paso anterior)
query {
  funkoById(id: <ID_CREADO>) {
    nombre
    precio
    category {
      nombre
    }
  }
}
```

### Test 2: Actualizar y verificar
```graphql
# Paso 1: Actualizar parcialmente
mutation {
  patchFunko(id: 1, input: { precio: 19.99 }) {
    id
    precio
    nombre
  }
}

# Paso 2: Verificar
query {
  funkoById(id: 1) {
    precio
    updatedAt
  }
}
```

### Test 3: Paginación
```graphql
# Primera página
query {
  allFunkosPaged(first: 3) {
    pageInfo {
      hasNextPage
      endCursor
    }
    edges {
      node { nombre }
    }
  }
}

# Segunda página (usa el endCursor del resultado anterior)
query {
  allFunkosPaged(first: 3, after: "<CURSOR_ANTERIOR>") {
    edges {
      node { nombre }
    }
  }
}
```

---

## ⚠️ Manejo de Errores

### Error: Categoría no existe
```graphql
mutation {
  createFunko(
    input: {
      nombre: "Test"
      categoria: "CATEGORIA_INEXISTENTE"
      precio: 10.0
    }
  ) {
    id
  }
}
```

**Respuesta esperada:**
```json
{
  "errors": [
    {
      "message": "La categoría: CATEGORIA_INEXISTENTE no existe.",
      "extensions": {
        "code": "FunkoApi.Error.FunkoConflictError"
      }
    }
  ]
}
```

### Error: Funko no encontrado
```graphql
query {
  funkoById(id: 99999) {
    nombre
  }
}
```

**Respuesta esperada:**
```json
{
  "data": {
    "getFunkoById": null
  }
}
```

---

## 🛠️ Herramientas Recomendadas

1. **Banana Cake Pop** (HotChocolate oficial)
   - URL: http://localhost:8080/graphql/
   - Incluye explorador de schema, autocompletado y soporte para subscriptions

2. **Altair GraphQL Client**
   - Extensión de navegador/app de escritorio
   - Excelente para subscriptions

3. **GraphQL Playground**
   - Cliente web clásico
   - Buen soporte para documentación

4. **Postman**
   - Versión 8+ tiene soporte para GraphQL
   - Bueno para testing automatizado

---

## 📝 Notas Importantes

- **Categorías válidas**: POKEMON, MARVEL, WOW, TERROR
- **IDs de Funkos**: Se usan enteros (long): 1, 2, 3...
- **IDs de Categorías**: Se usan UUIDs (Guid): "722f9661-8631-419b-8903-34e9e0339d01"
- **Subscriptions**: Requieren conexión WebSocket persistente
- **Paginación**: `first` = cantidad, `after` = cursor para siguiente página

---

## 🎯 Workflow Completo de Testing

```graphql
# 1. Ver todas las categorías disponibles
query {
  allCategories {
    id
    nombre
  }
}

# 2. Crear un Funko nuevo
mutation {
  createFunko(
    input: {
      nombre: "Gastly"
      categoria: "POKEMON"
      precio: 11.50
    }
  ) {
    id
    nombre
    categoria
  }
}

# 3. Actualizar precio
mutation {
  patchFunko(id: <ID_CREADO>, input: { precio: 12.99 }) {
    id
    precio
    nombre
  }
}

# 4. Ver todos los Funkos
query {
  allFunkos {
    nombre
    precio
    category { nombre }
  }
}

# 5. Eliminar el Funko
mutation {
  deleteFunko(id: <ID_CREADO>) {
    nombre
  }
}
```

¡Listo para probar tu API GraphQL! 🚀
