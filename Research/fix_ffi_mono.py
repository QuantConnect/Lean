# flake8: noqa

cdef = []

cdef.append(
    """
typedef struct _MonoDomain MonoDomain;
typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoImage MonoImage;
typedef struct _MonoMethodDesc MonoMethodDesc;
typedef struct _MonoMethod MonoMethod;
typedef struct _MonoObject MonoObject;

MonoDomain* mono_jit_init(const char *root_domain_name);
void mono_jit_cleanup(MonoDomain *domain);
MonoAssembly* mono_domain_assembly_open(MonoDomain *domain, const char *name);
MonoImage* mono_assembly_get_image(MonoAssembly *assembly);

void mono_config_parse(const char* path);
void mono_domain_set_config(MonoDomain *domain, const char *base_dir, const char *config_file_name);

MonoMethodDesc* mono_method_desc_new(const char* name, bool include_namespace);
MonoMethod* mono_method_desc_search_in_image(MonoMethodDesc *method_desc, MonoImage *image);
void mono_method_desc_free(MonoMethodDesc *method_desc);

MonoObject* mono_runtime_invoke(MonoMethod *method, void *obj, void **params, MonoObject **exc);

void* mono_object_unbox(MonoObject *object);
"""
)