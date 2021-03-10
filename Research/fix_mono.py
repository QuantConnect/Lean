import atexit

from .ffi import load_mono, ffi


__all__ = ["Mono"]


_MONO = None
_ROOT_DOMAIN = None


class Mono:
    def __init__(self, libmono, domain=None, config_file=None):
        self._assemblies = {}

        initialize(config_file=config_file, libmono=libmono)

        if domain is None:
            self._domain = _ROOT_DOMAIN
        else:
            raise NotImplementedError

    def get_callable(self, assembly_path, typename, function):
        assembly = self._assemblies.get(assembly_path)
        if not assembly:
            assembly = _MONO.mono_domain_assembly_open(
                self._domain, assembly_path.encode("utf8")
            )
            _check_result(assembly, f"Unable to load assembly {assembly_path}")
            self._assemblies[assembly_path] = assembly

        image = _MONO.mono_assembly_get_image(assembly)
        _check_result(image, "Unable to load image from assembly")

        desc = MethodDesc(typename, function)
        method = desc.search(image)
        _check_result(
            method, f"Could not find method {typename}.{function} in assembly"
        )

        return MonoMethod(method)


class MethodDesc:
    def __init__(self, typename, function):
        self._desc = f"{typename}:{function}"
        self._ptr = _MONO.mono_method_desc_new(
            self._desc.encode("utf8"), 1  # include_namespace
        )

    def search(self, image):
        return _MONO.mono_method_desc_search_in_image(self._ptr, image)

    def __del__(self):
        if _MONO:
            _MONO.mono_method_desc_free(self._ptr)


class MonoMethod:
    def __init__(self, ptr):
        self._ptr = ptr

    def __call__(self, ptr, size):
        exception = ffi.new("MonoObject**")
        params = ffi.new("void*[2]")

        # Keep these alive until the function is called by assigning them locally
        ptr_ptr = ffi.new("void**", ptr)
        size_ptr = ffi.new("int32_t*", size)

        params[0] = ptr_ptr
        params[1] = size_ptr

        res = _MONO.mono_runtime_invoke(self._ptr, ffi.NULL, params, exception)
        _check_result(res, "Failed to call method")

        unboxed = ffi.cast("int32_t*", _MONO.mono_object_unbox(res))
        _check_result(unboxed, "Failed to convert result to int")

        return unboxed[0]


def initialize(config_file: str, libmono: str) -> None:
    global _MONO, _ROOT_DOMAIN
    if _MONO is None:
        _MONO = load_mono(libmono)

        if config_file is None:
            config_bytes = ffi.NULL
        else:
            config_bytes = config_file.encode("utf8")

        _ROOT_DOMAIN = _MONO.mono_jit_init(b"clr_loader")

        _MONO.mono_domain_set_config(_ROOT_DOMAIN, b"/etc/mono/", b"config");

        _check_result(_ROOT_DOMAIN, "Failed to initialize Mono")
        atexit.register(_release)


def _release():
    global _MONO, _ROOT_DOMAIN
    if _ROOT_DOMAIN is not None and _MONO is not None:
        _MONO.mono_jit_cleanup(_ROOT_DOMAIN)
        _MONO = None
        _ROOT_DOMAIN = None


def _check_result(res, msg):
    if res == ffi.NULL or not res:
        raise RuntimeError(msg)