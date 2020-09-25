cmake_minimum_required (VERSION 3.17)
cmake_policy(SET CMP0015 NEW)

project("Profiler.Native.Tests" VERSION 1.0.0)

include(EnableGoogleTest.cmake)
include(GoogleTest)

add_compile_options(-std=c++11 -fPIC -fms-extensions -stdlib=libstdc++)
add_compile_options(-DBIT64 -DPAL_STDCPP_COMPAT -DPLATFORM_UNIX -DUNICODE)
add_compile_options(-Wno-invalid-noreturn -Wno-macro-redefined -Wno-pragma-pack)

set(GTEST_LIBRARY "${CMAKE_CURRENT_SOURCE_DIR}/lib/libgtest.a")
find_library(pthreads NAMES libpthread.so FIND_LIBRARY_USE_LIB64_PATHS)

SET_SOURCE_FILES_PROPERTIES(
  ${GENERATED_OBJ_FILES}
  PROPERTIES
  EXTERNAL_OBJECT false
  GENERATED true
)

add_executable(ProfilerTests
    dummy.cpp
    main.cpp
    ${GENERATED_OBJ_FILES}
)

target_include_directories(ProfilerTests
    PUBLIC googletest-src/googletest/include/
    PUBLIC .
)

target_link_libraries(ProfilerTests ${GTEST_LIBRARY} ${pthreads})

gtest_add_tests(TARGET      ProfilerTests
                TEST_SUFFIX .noArgs
                TEST_LIST   noArgsTests
)
